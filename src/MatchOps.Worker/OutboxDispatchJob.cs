// -----------------------------------------------------------------------------
// <copyright file="OutboxDispatchJob.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox 配信メッセージを定期的に処理するバックグラウンドジョブ（design.md §10）。
// Phase 0 はポーリング間隔で IOutboxDispatcher を呼ぶ（Phase 1 で Hangfire/cron に置換）。
// スコープ毎に DbContext 等を解決し、未処理メッセージを送信・記録・状態更新する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Notifications;
using MatchOps.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace MatchOps.Worker;

/// <summary>Outbox を定期処理するバックグラウンドジョブ。</summary>
public sealed class OutboxDispatchJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxDispatchJob> _logger;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="scopeFactory">スコープファクトリ（スコープ付き依存の解決に使用）。</param>
    /// <param name="options">Outbox 設定。</param>
    /// <param name="logger">ロガー。</param>
    public OutboxDispatchJob(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatchJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IOutboxDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
                OutboxDispatchSummary summary = await dispatcher
                    .DispatchPendingAsync(_options.BatchSize, stoppingToken)
                    .ConfigureAwait(false);

                if (summary.Sent + summary.Failed + summary.Skipped > 0)
                {
                    _logger.LogInformation(
                        "Outbox 処理: sent={Sent} failed={Failed} skipped={Skipped}",
                        summary.Sent,
                        summary.Failed,
                        summary.Skipped);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 1 回の失敗でジョブを止めず、次回ポーリングで再試行する。
                _logger.LogError(ex, "Outbox 処理中にエラーが発生しました。次回ポーリングで再試行します。");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
