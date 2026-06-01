// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaignConverters.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// MatchingCampaign 集約の永続化用 ValueConverter / ValueComparer。
// 対象枠リストと候補リスト（スコア内訳・提案理由を含む）を jsonb 列に保存する（設計 §8）。
// Domain は private ctor + factory を持つため、変換は「素朴表現 ↔ factory 再構築」で行う（Domain 非依存）。
// 候補は可変（提案理由の付与）のため、比較子のスナップショットはディープコピーで作り、変更検知を確実にする。
// </summary>
// -----------------------------------------------------------------------------

using System.Text.Json;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MatchOps.Infrastructure.Persistence.Conversions;

/// <summary>MatchingCampaign の値変換・比較子を提供する。</summary>
public static class MatchingCampaignConverters
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    /// <summary>対象枠リスト ↔ jsonb（Guid 配列）。</summary>
    public static ValueConverter<IReadOnlyList<TimeSlotId>, string> TargetSlots { get; } =
        new(
            slots => JsonSerializer.Serialize(slots.Select(slot => slot.Value), Json),
            value => JsonSerializer.Deserialize<List<Guid>>(value, Json)!
                .Select(id => new TimeSlotId(id))
                .ToList());

    /// <summary>対象枠リストの変更追跡用比較子。</summary>
    public static ValueComparer<IReadOnlyList<TimeSlotId>> TargetSlotsComparer { get; } =
        new(
            (left, right) => left!.SequenceEqual(right!),
            slots => slots.Aggregate(0, (hash, slot) => HashCode.Combine(hash, slot.GetHashCode())),
            slots => slots.ToList());

    /// <summary>候補リスト ↔ jsonb。</summary>
    public static ValueConverter<IReadOnlyList<MatchingCandidate>, string> Candidates { get; } =
        new(
            candidates => JsonSerializer.Serialize(candidates.Select(ToJson), Json),
            value => JsonSerializer.Deserialize<List<CandidateJson>>(value, Json)!
                .Select(FromJson)
                .ToList());

    /// <summary>候補リストの変更追跡用比較子（提案理由の付与を検知するためディープコピーでスナップショット）。</summary>
    public static ValueComparer<IReadOnlyList<MatchingCandidate>> CandidatesComparer { get; } =
        new(
            (left, right) => SameCandidates(left!, right!),
            candidates => candidates.Aggregate(
                0, (hash, candidate) => HashCode.Combine(hash, candidate.CustomerId, candidate.OfferId, candidate.ProposalReason)),
            candidates => candidates.Select(Clone).ToList());

    private static bool SameCandidates(
        IReadOnlyList<MatchingCandidate> left, IReadOnlyList<MatchingCandidate> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            MatchingCandidate a = left[index];
            MatchingCandidate b = right[index];
            if (a.CustomerId != b.CustomerId
                || a.TimeSlotId != b.TimeSlotId
                || a.OfferId != b.OfferId
                || a.ProposalReason != b.ProposalReason
                || a.Score.Value != b.Score.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static MatchingCandidate Clone(MatchingCandidate candidate) => FromJson(ToJson(candidate));

    private static CandidateJson ToJson(MatchingCandidate candidate) => new(
        candidate.CustomerId.Value,
        candidate.TimeSlotId.Value,
        candidate.OfferId.Value,
        new Dictionary<string, double>(candidate.Breakdown.Contributions, StringComparer.Ordinal),
        candidate.ProposalReason);

    private static MatchingCandidate FromJson(CandidateJson json)
    {
        MatchingCandidate candidate = MatchingCandidate.Create(
            new CustomerId(json.CustomerId),
            new TimeSlotId(json.TimeSlotId),
            new OfferId(json.OfferId),
            ScoreBreakdown.From(json.Contributions));

        if (!string.IsNullOrWhiteSpace(json.ProposalReason))
        {
            candidate.AttachProposalReason(json.ProposalReason);
        }

        return candidate;
    }

    private sealed record CandidateJson(
        Guid CustomerId,
        Guid TimeSlotId,
        Guid OfferId,
        Dictionary<string, double> Contributions,
        string? ProposalReason);
}
