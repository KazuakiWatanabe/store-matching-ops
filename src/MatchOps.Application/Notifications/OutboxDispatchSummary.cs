// -----------------------------------------------------------------------------
// <copyright file="OutboxDispatchSummary.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox ディスパッチ 1 回分の処理結果サマリ（送信成功・失敗・スキップ件数）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Notifications;

/// <summary>Outbox ディスパッチの処理結果サマリ。</summary>
/// <param name="Sent">送信に成功した件数。</param>
/// <param name="Failed">送信に失敗した件数（リトライ対象・恒久失敗を含む）。</param>
/// <param name="Skipped">配信制御によりスキップした件数。</param>
public sealed record OutboxDispatchSummary(int Sent, int Failed, int Skipped);
