// -----------------------------------------------------------------------------
// <copyright file="OutboxDispatcher.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IOutboxDispatcher の EF Core 実装（design.md §10）。
// 送信待ち（queued かつ再試行時刻到来）の Outbox メッセージを処理する。背景処理のためテナントフィルタを回避する。
// 送信前にオプトアウト等を再判定しスキップ、送信は INotificationSender、結果を notification_logs に記録（PII 非含）。
// 失敗は指数バックオフで再試行し、最大試行回数到達で恒久失敗（failed）にする。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;
using MatchOps.Application.Notifications;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchOps.Infrastructure.Notifications;

/// <summary>Outbox の配信メッセージを処理する EF Core 実装。</summary>
public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private const string StatusQueued = "queued";
    private const string StatusSent = "sent";
    private const string StatusFailed = "failed";
    private const string StatusSkipped = "skipped";

    private readonly MatchOpsDbContext _dbContext;
    private readonly INotificationSender _sender;
    private readonly INotificationEligibility _eligibility;
    private readonly IClock _clock;
    private readonly OutboxOptions _options;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <param name="sender">配信実装。</param>
    /// <param name="eligibility">配信可否判定。</param>
    /// <param name="clock">時刻源。</param>
    /// <param name="options">Outbox 設定。</param>
    /// <exception cref="ArgumentNullException">いずれかの依存が <c>null</c> の場合。</exception>
    public OutboxDispatcher(
        MatchOpsDbContext dbContext,
        INotificationSender sender,
        INotificationEligibility eligibility,
        IClock clock,
        IOptions<OutboxOptions> options)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<OutboxDispatchSummary> DispatchPendingAsync(
        int batchSize, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = _clock.Now;
        DateOnly today = _clock.Today;

        // 背景処理はテナント未解決のため、フィルタを回避して全テナント横断で処理する。
        List<OutboxMessageEntity> pending = await _dbContext.OutboxMessages
            .IgnoreQueryFilters()
            .Where(message => message.Status == StatusQueued
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int sent = 0;
        int failed = 0;
        int skipped = 0;

        foreach (OutboxMessageEntity message in pending)
        {
            bool allowed = await _eligibility
                .IsAllowedAsync(message.TenantId, message.CustomerId, today, cancellationToken)
                .ConfigureAwait(false);
            if (!allowed)
            {
                message.Status = StatusSkipped;
                AddLog(message, StatusSkipped, "配信制御によりスキップしました。", now);
                skipped++;
                continue;
            }

            NotificationSendResult result = await _sender
                .SendAsync(ToMessage(message), cancellationToken)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                message.Status = StatusSent;
                AddLog(message, StatusSent, null, now);
                sent++;
            }
            else
            {
                ApplyFailure(message, result.Error, now);
                AddLog(message, StatusFailed, FailureDetail(message, result.Error), now);
                failed++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new OutboxDispatchSummary(sent, failed, skipped);
    }

    private void ApplyFailure(OutboxMessageEntity message, string? error, DateTimeOffset now)
    {
        message.AttemptCount++;
        if (message.AttemptCount >= _options.MaxAttempts)
        {
            // 最大試行回数に到達したため恒久失敗（以後再試行しない）。
            message.Status = StatusFailed;
            message.NextAttemptAt = null;
        }
        else
        {
            // 指数バックオフで再試行（queued のまま次回試行時刻を設定）。
            double backoffSeconds = _options.BaseBackoffSeconds * Math.Pow(2, message.AttemptCount - 1);
            message.NextAttemptAt = now.AddSeconds(backoffSeconds);
        }
    }

    private string FailureDetail(OutboxMessageEntity message, string? error)
        => message.Status == StatusFailed
            ? $"最大試行回数（{_options.MaxAttempts}）に到達: {error}"
            : $"再試行予定（試行 {message.AttemptCount} 回目）: {error}";

    private void AddLog(OutboxMessageEntity message, string status, string? detail, DateTimeOffset now)
        => _dbContext.NotificationLogs.Add(new NotificationLogEntry
        {
            TenantId = message.TenantId,
            OutboxMessageId = message.Id,
            CampaignId = message.CampaignId,
            CustomerId = message.CustomerId,
            Status = status,
            Detail = detail,
            OccurredAt = now,
        });

    private static NotificationMessage ToMessage(OutboxMessageEntity message)
        => new(
            message.TenantId,
            message.CampaignId,
            message.CustomerId,
            message.OfferId,
            message.TimeSlotId,
            message.Body);
}
