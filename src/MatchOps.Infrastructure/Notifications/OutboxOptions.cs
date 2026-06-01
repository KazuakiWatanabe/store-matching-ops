// -----------------------------------------------------------------------------
// <copyright file="OutboxOptions.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox ディスパッチの設定（最大試行回数・バッチ件数・指数バックオフ基準秒・ポーリング間隔）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Infrastructure.Notifications;

/// <summary>Outbox ディスパッチの設定。</summary>
public sealed class OutboxOptions
{
    /// <summary>設定セクション名。</summary>
    public const string SectionName = "Outbox";

    /// <summary>送信の最大試行回数（到達で恒久失敗）。</summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>1 回のディスパッチで処理する最大件数。</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>指数バックオフの基準秒（再試行間隔 = 基準秒 × 2^(試行回数-1)）。</summary>
    public int BaseBackoffSeconds { get; init; } = 30;

    /// <summary>ワーカーのポーリング間隔（秒）。</summary>
    public int PollIntervalSeconds { get; init; } = 30;
}
