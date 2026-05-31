// -----------------------------------------------------------------------------
// <copyright file="TimeSlot.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 空き枠 (TimeSlot) Aggregate Root。リソース × 時間範囲の供給単位を表す。
// 状態遷移: open → held（仮押さえ）→ booked（確定）/ open（解放）。closed は手動/期限切れ。
// 不正遷移は DomainException。予約確定の副作用（通知等）は Domain に持ち込まない（Application で調整）。
// 関連: ADR-0009 (状態遷移), ADR-0006 (テナント分離), 設計 §4-5
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Scheduling;

/// <summary>空き枠を表す Aggregate Root。</summary>
public sealed class TimeSlot
{
    private static readonly IReadOnlySet<string> NoCategories = new HashSet<string>(StringComparer.Ordinal);

    private TimeSlot(
        TimeSlotId id,
        TenantId tenantId,
        StoreId storeId,
        ResourceId resourceId,
        TimeRange range,
        IReadOnlySet<string> supportedOfferCategories,
        SlotStatus status)
    {
        Id = id;
        TenantId = tenantId;
        StoreId = storeId;
        ResourceId = resourceId;
        Range = range;
        SupportedOfferCategories = supportedOfferCategories;
        Status = status;
    }

    /// <summary>空き枠の一意識別子。</summary>
    public TimeSlotId Id { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>所属店舗。</summary>
    public StoreId StoreId { get; }

    /// <summary>割り当てリソース。</summary>
    public ResourceId ResourceId { get; }

    /// <summary>時間範囲。</summary>
    public TimeRange Range { get; }

    /// <summary>対応する Offer 種別（カテゴリタグ。Catalog と段階的に整合）。</summary>
    public IReadOnlySet<string> SupportedOfferCategories { get; }

    /// <summary>現在の状態。</summary>
    public SlotStatus Status { get; private set; }

    /// <summary>
    /// 指定リソースに対して公開状態 (open) の空き枠を新規に開く。
    /// リソースのテナント・店舗が引数と一致しない場合は不変条件違反として拒否する（ADR-0006）。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="resource">割り当てリソース。</param>
    /// <param name="range">時間範囲。</param>
    /// <param name="supportedOfferCategories">対応 Offer 種別タグ（任意）。</param>
    /// <returns>open 状態の <see cref="TimeSlot"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> が <c>null</c> の場合。</exception>
    /// <exception cref="DomainException">リソースのテナントまたは店舗が一致しない、または対応種別タグが空の場合。</exception>
    public static TimeSlot Open(
        TenantId tenantId,
        StoreId storeId,
        Resource resource,
        TimeRange range,
        IEnumerable<string>? supportedOfferCategories = null)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource.TenantId != tenantId)
        {
            throw new DomainException("リソースのテナントが一致しません。");
        }

        if (resource.StoreId != storeId)
        {
            throw new DomainException("リソースの店舗が一致しません。");
        }

        return new TimeSlot(
            TimeSlotId.New(),
            tenantId,
            storeId,
            resource.Id,
            range,
            NormalizeCategories(supportedOfferCategories),
            SlotStatus.Open);
    }

    /// <summary>open → held（仮押さえ）。</summary>
    /// <exception cref="DomainException">open 以外の状態の場合。</exception>
    public void Hold() => Transition(SlotStatus.Open, SlotStatus.Held, "仮押さえ");

    /// <summary>held → booked（確定）。確定の副作用は Application 層で扱う。</summary>
    /// <exception cref="DomainException">held 以外の状態の場合。</exception>
    public void Book() => Transition(SlotStatus.Held, SlotStatus.Booked, "確定");

    /// <summary>held → open（仮押さえの解放）。</summary>
    /// <exception cref="DomainException">held 以外の状態の場合。</exception>
    public void Release() => Transition(SlotStatus.Held, SlotStatus.Open, "解放");

    /// <summary>枠をクローズする（手動/期限切れ）。クローズ済みの再クローズは不可。</summary>
    /// <exception cref="DomainException">既に closed の場合。</exception>
    public void Close()
    {
        if (Status == SlotStatus.Closed)
        {
            throw new DomainException("既にクローズ済みの枠です。");
        }

        Status = SlotStatus.Closed;
    }

    /// <summary>
    /// 同一リソースかつ時間範囲が重複するかを返す（重複枠検出の基礎）。
    /// </summary>
    /// <param name="other">比較対象の空き枠。</param>
    /// <returns>同一リソースで時間が重複する場合は <c>true</c>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> が <c>null</c> の場合。</exception>
    public bool OverlapsWith(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return ResourceId == other.ResourceId && Range.Overlaps(other.Range);
    }

    private void Transition(SlotStatus from, SlotStatus to, string action)
    {
        if (Status != from)
        {
            throw new DomainException($"{Status} 状態の枠は{action}できません。");
        }

        Status = to;
    }

    private static IReadOnlySet<string> NormalizeCategories(IEnumerable<string>? categories)
    {
        if (categories is null)
        {
            return NoCategories;
        }

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (string category in categories)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new DomainException("対応 Offer 種別が空です。");
            }

            set.Add(category.Trim());
        }

        return set;
    }
}
