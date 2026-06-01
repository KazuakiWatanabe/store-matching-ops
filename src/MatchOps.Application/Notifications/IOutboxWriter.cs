// -----------------------------------------------------------------------------
// <copyright file="IOutboxWriter.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox への積み込み抽象。配信は実送信せず、ここに積むだけ（承認済み施策のみ・design.md §10）。
// 実装は Infrastructure（EF Core で outbox_messages に INSERT）。確定は IUnitOfWork に委ねる。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Notifications;

/// <summary>配信メッセージを Outbox に積む抽象。</summary>
public interface IOutboxWriter
{
    /// <summary>
    /// 配信メッセージを Outbox に積む。実送信はしない（Worker が後で実送信する）。
    /// 確定は呼び出し側の <see cref="MatchOps.Application.Common.IUnitOfWork"/> による。
    /// </summary>
    /// <param name="message">積み込む配信メッセージ。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>積み込み処理を表すタスク。</returns>
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
