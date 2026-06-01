// -----------------------------------------------------------------------------
// <copyright file="IdempotencyRecord.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 冪等キーに紐づく保存済みレスポンス（CLAUDE.md §10.1）。
// リクエスト本文のハッシュを保持し、同一キー＋異なる本文を 409 で弾く判定に用いる。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Idempotency;

/// <summary>冪等キーに紐づく保存済みレスポンス。</summary>
/// <param name="RequestHash">リクエスト本文のハッシュ（同一キーでの本文差異検出に使用）。</param>
/// <param name="StatusCode">保存した HTTP ステータスコード。</param>
/// <param name="Body">保存したレスポンス本文（JSON。本文なしは <c>null</c>）。</param>
/// <param name="CreatedAt">保存日時（TTL 判定の起点）。</param>
public sealed record IdempotencyRecord(
    string RequestHash,
    int StatusCode,
    string? Body,
    DateTimeOffset CreatedAt);
