// -----------------------------------------------------------------------------
// <copyright file="ICampaignCandidateSource.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策の候補抽出入力を各モジュール（Customers / Scheduling / Catalog）から組み立てる抽象。
// モジュール間連携は Application インターフェース経由とし、別モジュールの Domain 型を跨いで参照しない（CLAUDE.md §3.2, §4.1）。
// 実装（Infrastructure）はテナントスコープ内のデータのみ返す（ADR-0006）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>施策の候補抽出入力を組み立てる抽象。</summary>
public interface ICampaignCandidateSource
{
    /// <summary>
    /// 対象テナント・店舗・空き枠群に対する、枠ごとの候補抽出入力を組み立てて返す。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="targetSlots">対象の空き枠群。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>枠ごとの候補抽出入力。</returns>
    Task<IReadOnlyList<SlotCandidates>> GetAsync(
        TenantId tenantId,
        StoreId storeId,
        IReadOnlyList<TimeSlotId> targetSlots,
        CancellationToken cancellationToken = default);
}
