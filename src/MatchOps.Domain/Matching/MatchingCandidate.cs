// -----------------------------------------------------------------------------
// <copyright file="MatchingCandidate.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// マッチング候補。顧客 × 空き枠 × Offer のスコアと内訳を保持する。
// スコア (MatchScore) は内訳 (ScoreBreakdown) の合計から導出し、常に整合する。
// 提案理由・文面は後続（009, 施策単位）で付与する格納先のみを持つ。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>顧客 × 空き枠 × Offer のマッチング候補。</summary>
public sealed class MatchingCandidate
{
    private MatchingCandidate(CustomerId customerId, TimeSlotId timeSlotId, OfferId offerId, ScoreBreakdown breakdown)
    {
        CustomerId = customerId;
        TimeSlotId = timeSlotId;
        OfferId = offerId;
        Breakdown = breakdown;
    }

    /// <summary>候補の顧客。</summary>
    public CustomerId CustomerId { get; }

    /// <summary>対象の空き枠。</summary>
    public TimeSlotId TimeSlotId { get; }

    /// <summary>提案する Offer。</summary>
    public OfferId OfferId { get; }

    /// <summary>スコアの内訳。</summary>
    public ScoreBreakdown Breakdown { get; }

    /// <summary>来店可能性スコア（内訳の合計から導出）。</summary>
    public MatchScore Score => Breakdown.Total;

    /// <summary>提案理由・文面（後続で付与。未設定なら <c>null</c>）。</summary>
    public string? ProposalReason { get; private set; }

    /// <summary>
    /// 候補を生成する。スコアは内訳の合計から導出する。
    /// </summary>
    /// <param name="customerId">候補の顧客。</param>
    /// <param name="timeSlotId">対象の空き枠。</param>
    /// <param name="offerId">提案する Offer。</param>
    /// <param name="breakdown">スコア内訳。</param>
    /// <returns>生成された <see cref="MatchingCandidate"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="breakdown"/> が <c>null</c> の場合。</exception>
    public static MatchingCandidate Create(
        CustomerId customerId, TimeSlotId timeSlotId, OfferId offerId, ScoreBreakdown breakdown)
    {
        ArgumentNullException.ThrowIfNull(breakdown);
        return new MatchingCandidate(customerId, timeSlotId, offerId, breakdown);
    }

    /// <summary>提案理由・文面を付与する（承認前の編集を含む）。</summary>
    /// <param name="reason">提案理由・文面。</param>
    /// <exception cref="DomainException">理由が空白の場合。</exception>
    public void AttachProposalReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("提案理由は空にできません。");
        }

        ProposalReason = reason.Trim();
    }
}
