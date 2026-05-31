// -----------------------------------------------------------------------------
// <copyright file="CustomerActivity.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客の行動履歴 (来店・注文・施術・予約・キャンセル) を表す append-only な Entity。
// 生成後は不変。テナント・顧客に紐付き、来店統計の導出元となる（設計 §5）。
// 時刻は IClock を知らず、発生日をパラメータで受け取る（CLAUDE.md §10.4）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Customers;

/// <summary>顧客の行動履歴を表す不変 (append-only) の Entity。</summary>
public sealed class CustomerActivity
{
    private CustomerActivity(
        CustomerActivityId id,
        TenantId tenantId,
        CustomerId customerId,
        ActivityType type,
        DateOnly occurredOn,
        Money? amount)
    {
        Id = id;
        TenantId = tenantId;
        CustomerId = customerId;
        Type = type;
        OccurredOn = occurredOn;
        Amount = amount;
    }

    /// <summary>行動履歴の一意識別子。</summary>
    public CustomerActivityId Id { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>対象顧客。</summary>
    public CustomerId CustomerId { get; }

    /// <summary>行動の種別。</summary>
    public ActivityType Type { get; }

    /// <summary>行動が発生した日。</summary>
    public DateOnly OccurredOn { get; }

    /// <summary>金額（注文・会計等。該当しない場合は <c>null</c>）。</summary>
    public Money? Amount { get; }

    /// <summary>
    /// 新しい行動履歴を記録（生成）する。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="customerId">対象顧客。</param>
    /// <param name="type">行動の種別。</param>
    /// <param name="occurredOn">行動が発生した日。</param>
    /// <param name="amount">金額（任意）。</param>
    /// <returns>生成された <see cref="CustomerActivity"/>。</returns>
    public static CustomerActivity Record(
        TenantId tenantId,
        CustomerId customerId,
        ActivityType type,
        DateOnly occurredOn,
        Money? amount = null)
        => new(CustomerActivityId.New(), tenantId, customerId, type, occurredOn, amount);
}
