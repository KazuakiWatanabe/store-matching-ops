// -----------------------------------------------------------------------------
// <copyright file="IOutboxDispatcher.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox に積まれた配信メッセージを処理する抽象（design.md §10）。
// 送信可能（キュー状態・再試行時刻到来）かつ配信可（オプトアウト等でない）なメッセージを実送信し、
// 配信ログを記録、結果に応じて状態更新（成功=sent / 失敗=リトライ or 恒久失敗）する。
// Phase 0 は cron/手動トリガ、Phase 1 で Hangfire。実装は Infrastructure。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Notifications;

/// <summary>Outbox の配信メッセージを処理する抽象。</summary>
public interface IOutboxDispatcher
{
    /// <summary>
    /// 送信待ちの Outbox メッセージを最大 <paramref name="batchSize"/> 件処理する。
    /// </summary>
    /// <param name="batchSize">1 回で処理する最大件数。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>処理結果サマリ。</returns>
    Task<OutboxDispatchSummary> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken = default);
}
