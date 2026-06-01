// -----------------------------------------------------------------------------
// <copyright file="LoggingNotificationSender.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// INotificationSender の Phase 0 スタブ実装。実 LINE/メール API を叩かず、配信をログ出力するのみ（成功扱い）。
// 連絡先等の PII を平文でログに出さない（識別子と本文長のみ。CLAUDE.md §9.2）。Phase 1 で実送信に差し替える。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace MatchOps.Infrastructure.Notifications;

/// <summary>ログ出力のみを行う Phase 0 の配信スタブ。</summary>
public sealed class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="logger">ロガー（PII を出力しない）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> が <c>null</c> の場合。</exception>
    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task<NotificationSendResult> SendAsync(
        NotificationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // 識別子と本文長のみを記録（連絡先・本文の平文は出さない）。
        _logger.LogInformation(
            "配信(スタブ): campaign={CampaignId} customer={CustomerId} offer={OfferId} bodyLength={BodyLength}",
            message.CampaignId,
            message.CustomerId,
            message.OfferId,
            message.Body.Length);

        return Task.FromResult(NotificationSendResult.Success());
    }
}
