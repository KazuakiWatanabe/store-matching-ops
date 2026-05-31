// -----------------------------------------------------------------------------
// <copyright file="Offer.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 提案内容 (Offer) Aggregate Root。メニュー・コース・クーポンを統一表現する。
// クーポンは値引き上限 (DiscountCap) を必須で保持し、提案値引きが上限を超えないことを保証する。
// 有効/無効と適用条件を持ち、無効・条件外の Offer は提案候補から除外する基礎を提供する。
// 関連: ADR-0010 (値引き上限・配信制御), ADR-0006 (テナント分離), 設計 §4/§6.3
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Catalog;

/// <summary>提案内容を表す Aggregate Root。</summary>
public sealed class Offer
{
    private Offer(
        OfferId id,
        TenantId tenantId,
        StoreId storeId,
        OfferType type,
        string name,
        DiscountCap? discountCap,
        OfferConditions conditions,
        bool isActive)
    {
        Id = id;
        TenantId = tenantId;
        StoreId = storeId;
        Type = type;
        Name = name;
        DiscountCap = discountCap;
        Conditions = conditions;
        IsActive = isActive;
    }

    /// <summary>Offer の一意識別子。</summary>
    public OfferId Id { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>所属店舗。</summary>
    public StoreId StoreId { get; }

    /// <summary>Offer 種別。</summary>
    public OfferType Type { get; }

    /// <summary>表示名。</summary>
    public string Name { get; }

    /// <summary>値引き上限（クーポンのみ。メニュー/コースは <c>null</c>）。</summary>
    public DiscountCap? DiscountCap { get; }

    /// <summary>適用条件（曜日・対象セグメント）。</summary>
    public OfferConditions Conditions { get; }

    /// <summary>有効かどうか。</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// クーポンを作成する（値引き上限は必須）。生成時は有効。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="name">表示名（必須）。</param>
    /// <param name="discountCap">値引き上限。</param>
    /// <param name="conditions">適用条件（任意。既定は制限なし）。</param>
    /// <returns>生成された有効なクーポン <see cref="Offer"/>。</returns>
    /// <exception cref="DomainException">表示名が空白の場合。</exception>
    public static Offer CreateCoupon(
        TenantId tenantId,
        StoreId storeId,
        string name,
        DiscountCap discountCap,
        OfferConditions? conditions = null)
    {
        EnsureNameProvided(name);
        return new Offer(
            OfferId.New(), tenantId, storeId, OfferType.Coupon, name.Trim(),
            discountCap, conditions ?? OfferConditions.Unrestricted, isActive: true);
    }

    /// <summary>
    /// メニューまたはコースを作成する（値引き上限を持たない）。生成時は有効。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="type">種別（<see cref="OfferType.Menu"/> または <see cref="OfferType.Course"/>）。</param>
    /// <param name="name">表示名（必須）。</param>
    /// <param name="conditions">適用条件（任意。既定は制限なし）。</param>
    /// <returns>生成された有効な <see cref="Offer"/>。</returns>
    /// <exception cref="DomainException">表示名が空白、または種別がクーポンの場合。</exception>
    public static Offer CreateItem(
        TenantId tenantId,
        StoreId storeId,
        OfferType type,
        string name,
        OfferConditions? conditions = null)
    {
        EnsureNameProvided(name);
        if (type == OfferType.Coupon)
        {
            throw new DomainException("クーポンは CreateCoupon を使用してください。");
        }

        return new Offer(
            OfferId.New(), tenantId, storeId, type, name.Trim(),
            discountCap: null, conditions ?? OfferConditions.Unrestricted, isActive: true);
    }

    /// <summary>Offer を有効化する。</summary>
    public void Activate() => IsActive = true;

    /// <summary>Offer を無効化する（以降は提案候補に出ない）。</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// 提案された値引きが上限を超えないことを保証する（CLAUDE.md §4.3）。
    /// </summary>
    /// <param name="proposed">提案された値引き。</param>
    /// <exception cref="DomainException">値引き上限を持たない Offer、または上限を超える場合。</exception>
    public void EnsureDiscountWithinCap(Discount proposed)
    {
        if (DiscountCap is not { } cap)
        {
            throw new DomainException("この Offer は値引きを持ちません。");
        }

        cap.EnsureWithin(proposed);
    }

    /// <summary>
    /// 指定日に提案候補として利用可能かを返す（有効かつ曜日条件を満たす）。
    /// セグメント条件は候補抽出時に <see cref="OfferConditions.TargetsSegment"/> で別途判定する。
    /// </summary>
    /// <param name="date">対象日。</param>
    /// <returns>利用可能なら <c>true</c>。</returns>
    public bool IsAvailableOn(DateOnly date) => IsActive && Conditions.AppliesOn(date);

    private static void EnsureNameProvided(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Offer の表示名は必須です。");
        }
    }
}
