// -----------------------------------------------------------------------------
// <copyright file="INotificationEligibility.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 配信直前の送信可否判定（配信制御, CLAUDE.md §4.3）。オプトアウト顧客・通知頻度上限超過は送信前にスキップする。
// 候補抽出時（MatchingEngine）にも判定するが、承認〜配信までの時間経過でオプトアウト等が変わりうるため送信直前に再判定する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Notifications;

/// <summary>配信直前の送信可否を判定する抽象。</summary>
public interface INotificationEligibility
{
    /// <summary>
    /// 指定顧客への配信が許可されるか（オプトイン済みかつ頻度上限内）を判定する。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="customerId">対象顧客。</param>
    /// <param name="today">基準日（頻度判定）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>許可されるなら <c>true</c>。</returns>
    Task<bool> IsAllowedAsync(
        TenantId tenantId, CustomerId customerId, DateOnly today, CancellationToken cancellationToken = default);
}
