// -----------------------------------------------------------------------------
// <copyright file="SlotScheduler.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 空き枠の開設を担うドメインサービス。単一 Aggregate では表現できない
// 「同一リソース・同一時間帯の重複枠を作らない」横断不変条件を検証する（ADR-0009）。
// クローズ済み枠はリソースを占有しないため重複対象から除外する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Scheduling;

/// <summary>重複枠を作らずに空き枠を開設するドメインサービス。</summary>
public sealed class SlotScheduler
{
    /// <summary>
    /// 既存枠と重複しないことを検証したうえで、open 状態の空き枠を開設する。
    /// </summary>
    /// <param name="existingSlotsForResource">対象リソースの既存枠（クローズ済みを含んでよい）。</param>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="resource">割り当てリソース。</param>
    /// <param name="range">時間範囲。</param>
    /// <param name="supportedOfferCategories">対応 Offer 種別タグ（任意）。</param>
    /// <returns>open 状態の <see cref="TimeSlot"/>。</returns>
    /// <exception cref="ArgumentNullException">引数が <c>null</c> の場合。</exception>
    /// <exception cref="DomainException">テナント不一致、または既存の非クローズ枠と重複する場合。</exception>
    public TimeSlot OpenSlot(
        IReadOnlyCollection<TimeSlot> existingSlotsForResource,
        TenantId tenantId,
        StoreId storeId,
        Resource resource,
        TimeRange range,
        IEnumerable<string>? supportedOfferCategories = null)
    {
        ArgumentNullException.ThrowIfNull(existingSlotsForResource);

        TimeSlot candidate = TimeSlot.Open(tenantId, storeId, resource, range, supportedOfferCategories);

        foreach (TimeSlot existing in existingSlotsForResource)
        {
            if (existing.Status == SlotStatus.Closed)
            {
                continue;
            }

            if (candidate.OverlapsWith(existing))
            {
                throw new DomainException("同一リソース・同一時間帯に重複する枠が既に存在します。");
            }
        }

        return candidate;
    }
}
