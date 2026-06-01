// -----------------------------------------------------------------------------
// <copyright file="CustomerNotificationEligibility.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// INotificationEligibility の Phase 0 実装。配信直前に顧客のオプトイン状態を再確認する（オプトアウトはスキップ）。
// 背景処理のためテナントフィルタを回避（IgnoreQueryFilters）し、テナント＋顧客 ID で照合する。
// 通知頻度上限は候補抽出時（MatchingEngine）に適用済み。頻度の送信直前再判定（最終通知日）は Phase 1 で拡張する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Notifications;
using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Notifications;

/// <summary>顧客のオプトイン状態に基づく配信可否判定（Phase 0）。</summary>
public sealed class CustomerNotificationEligibility : INotificationEligibility
{
    private readonly MatchOpsDbContext _dbContext;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> が <c>null</c> の場合。</exception>
    public CustomerNotificationEligibility(MatchOpsDbContext dbContext)
        => _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<bool> IsAllowedAsync(
        TenantId tenantId, CustomerId customerId, DateOnly today, CancellationToken cancellationToken = default)
    {
        // 背景処理はテナント未解決のため、フィルタを回避してテナント＋ID で明示照合する。
        Customer? customer = await _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == customerId, cancellationToken)
            .ConfigureAwait(false);

        return customer is not null && customer.CanReceiveNotifications();
    }
}
