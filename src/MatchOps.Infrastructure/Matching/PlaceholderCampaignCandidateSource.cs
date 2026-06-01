// -----------------------------------------------------------------------------
// <copyright file="PlaceholderCampaignCandidateSource.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ICampaignCandidateSource の暫定実装。対象枠を空候補で返す。
// セグメント抽出（休眠/常連/新規/平日利用者 等）とスコア特徴量（休眠日数・来店周期乖離・曜日時間帯一致）の
// 本実装は Phase 1（TASKS §3「セグメント抽出の本実装」「スコアの段階的拡張」）で行う。
// 本暫定により施策フロー（run→propose→approve→send→results）は通るが、候補は 0 件となる。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Matching;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Infrastructure.Matching;

/// <summary>Phase 1 本実装までの暫定候補ソース（空候補）。</summary>
public sealed class PlaceholderCampaignCandidateSource : ICampaignCandidateSource
{
    /// <inheritdoc />
    public Task<IReadOnlyList<SlotCandidates>> GetAsync(
        TenantId tenantId,
        StoreId storeId,
        IReadOnlyList<TimeSlotId> targetSlots,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetSlots);
        IReadOnlyList<SlotCandidates> slots = targetSlots
            .Select(slotId => new SlotCandidates(
                new SlotCandidacy(slotId, tenantId, storeId),
                Array.Empty<CandidateInput>()))
            .ToList();

        return Task.FromResult(slots);
    }
}
