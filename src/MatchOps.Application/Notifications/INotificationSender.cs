// -----------------------------------------------------------------------------
// <copyright file="INotificationSender.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 配信メッセージの実送信抽象。実装は Infrastructure に隔離する。
// Phase 0 は CSV 出力 or ログ出力のスタブ（実 LINE/メール API を叩かない）。Phase 1 で LINE/メール実装に差し替える。
// 連絡先等の PII をログに平文で出さない（CLAUDE.md §9.2）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Notifications;

/// <summary>配信メッセージを実送信する抽象。</summary>
public interface INotificationSender
{
    /// <summary>配信メッセージを送信する。</summary>
    /// <param name="message">配信メッセージ（PII を含まない）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>送信結果。</returns>
    Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
