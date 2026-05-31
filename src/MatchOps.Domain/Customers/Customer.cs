// -----------------------------------------------------------------------------
// <copyright file="Customer.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客 (Requester) Aggregate Root。テナント・店舗に属し、連絡先はハッシュのみ保持する。
// 来店統計 (回数・最終来店日) は活動履歴から集計列として保持し、RecordActivity で更新する。
// 配信可否はオプトイン状態に従う（オプトアウト尊重・要オプトイン）。
// 関連: ADR-0005 (PII/AI 境界), ADR-0006 (テナント分離), 設計 §5/§9
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Customers;

/// <summary>顧客を表す Aggregate Root。</summary>
public sealed class Customer
{
    private Customer(
        CustomerId id,
        TenantId tenantId,
        StoreId storeId,
        string displayName,
        ContactHash? phoneHash,
        ContactHash? emailHash,
        OptInStatus optInStatus)
    {
        Id = id;
        TenantId = tenantId;
        StoreId = storeId;
        DisplayName = displayName;
        PhoneHash = phoneHash;
        EmailHash = emailHash;
        OptInStatus = optInStatus;
    }

    /// <summary>顧客の一意識別子。</summary>
    public CustomerId Id { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>所属店舗。</summary>
    public StoreId StoreId { get; }

    /// <summary>表示名（PII。ログ・例外に出力しない）。</summary>
    public string DisplayName { get; }

    /// <summary>電話番号のハッシュ（未登録の場合は <c>null</c>）。</summary>
    public ContactHash? PhoneHash { get; }

    /// <summary>メールアドレスのハッシュ（未登録の場合は <c>null</c>）。</summary>
    public ContactHash? EmailHash { get; }

    /// <summary>来店回数（来店活動の集計）。</summary>
    public int VisitCount { get; private set; }

    /// <summary>最終来店日（来店がない場合は <c>null</c>）。</summary>
    public DateOnly? LastVisitOn { get; private set; }

    /// <summary>配信オプトイン状態。</summary>
    public OptInStatus OptInStatus { get; private set; }

    /// <summary>最終通知日（頻度制御用。未通知の場合は <c>null</c>）。</summary>
    public DateOnly? LastNotifiedOn { get; private set; }

    /// <summary>
    /// 顧客を新規登録（生成）する。連絡先はハッシュのみ受け取る。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="displayName">表示名（必須）。</param>
    /// <param name="phoneHash">電話番号のハッシュ（任意）。</param>
    /// <param name="emailHash">メールアドレスのハッシュ（任意）。</param>
    /// <param name="optInStatus">配信オプトイン状態（既定は未確認）。</param>
    /// <returns>生成された <see cref="Customer"/>。</returns>
    /// <exception cref="DomainException">表示名が空白の場合。</exception>
    public static Customer Register(
        TenantId tenantId,
        StoreId storeId,
        string displayName,
        ContactHash? phoneHash = null,
        ContactHash? emailHash = null,
        OptInStatus optInStatus = OptInStatus.Unknown)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("表示名は必須です。");
        }

        return new Customer(
            CustomerId.New(), tenantId, storeId, displayName.Trim(), phoneHash, emailHash, optInStatus);
    }

    /// <summary>オプトアウト済みかどうか。</summary>
    public bool IsOptedOut => OptInStatus == OptInStatus.OptedOut;

    /// <summary>
    /// 配信可能かどうか。明示的にオプトインした顧客のみ <c>true</c>
    /// （未確認・オプトアウトは配信しない）。
    /// </summary>
    /// <returns>配信可能なら <c>true</c>。</returns>
    public bool CanReceiveNotifications() => OptInStatus == OptInStatus.OptedIn;

    /// <summary>配信に同意（オプトイン）する。</summary>
    public void OptIn() => OptInStatus = OptInStatus.OptedIn;

    /// <summary>配信を拒否（オプトアウト）する。</summary>
    public void OptOut() => OptInStatus = OptInStatus.OptedOut;

    /// <summary>
    /// 行動履歴をこの顧客に適用し、来店統計を更新する。
    /// テナント・顧客が一致しない履歴は不変条件違反として拒否する（ADR-0006）。
    /// </summary>
    /// <param name="activity">適用する行動履歴。</param>
    /// <exception cref="ArgumentNullException"><paramref name="activity"/> が <c>null</c> の場合。</exception>
    /// <exception cref="DomainException">テナントまたは顧客が一致しない場合。</exception>
    public void RecordActivity(CustomerActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (activity.TenantId != TenantId)
        {
            throw new DomainException("異なるテナントの活動履歴は紐付けできません。");
        }

        if (activity.CustomerId != Id)
        {
            throw new DomainException("別の顧客の活動履歴は紐付けできません。");
        }

        if (activity.Type == ActivityType.Visit)
        {
            VisitCount++;
            if (LastVisitOn is null || activity.OccurredOn > LastVisitOn.Value)
            {
                LastVisitOn = activity.OccurredOn;
            }
        }
    }

    /// <summary>
    /// 指定日時点の休眠日数（最終来店日からの経過日数）を返す。
    /// 来店履歴がない場合は <c>null</c>。v0 スコアの休眠日数要素に用いる（ADR-0003）。
    /// </summary>
    /// <param name="today">基準日。</param>
    /// <returns>経過日数。来店がない場合は <c>null</c>。</returns>
    public int? DaysSinceLastVisit(DateOnly today)
        => LastVisitOn is { } lastVisit ? today.DayNumber - lastVisit.DayNumber : null;

    /// <summary>通知を送信したことを記録する（頻度制御用）。</summary>
    /// <param name="on">通知日。</param>
    public void MarkNotified(DateOnly on) => LastNotifiedOn = on;
}
