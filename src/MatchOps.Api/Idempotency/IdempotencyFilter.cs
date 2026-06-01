// -----------------------------------------------------------------------------
// <copyright file="IdempotencyFilter.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Idempotency-Key を強制する Action フィルタ（CLAUDE.md §10.1, design.md §10）。
// - run / approve / send 等の変更系に付与する。キー欠如は 400。
// - 24 時間以内の同一キー＋同一リクエスト本文 → 保存済みレスポンスを再生（再実行しない＝副作用 1 回）。
// - 同一キー＋異なる本文 → 409。
// キーはテナントでスコープする。サーバエラー(5xx)・例外はキャッシュしない。
// </summary>
// -----------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MatchOps.Api.Contracts;
using MatchOps.Api.Tenancy;
using MatchOps.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MatchOps.Api.Idempotency;

/// <summary>Idempotency-Key を強制し、二重実行を防ぐ Action フィルタ。</summary>
public sealed class IdempotencyFilter : IAsyncActionFilter
{
    /// <summary>冪等キーを渡すヘッダ名。</summary>
    public const string HeaderName = "Idempotency-Key";

    // 保存・再生するレスポンス本文は MVC（Web 既定 = camelCase）と同じ表記にする。
    private static readonly JsonSerializerOptions ResponseJson = new(JsonSerializerDefaults.Web);

    private readonly IIdempotencyStore _store;
    private readonly IClock _clock;
    private readonly RequestContext _requestContext;

    /// <summary>依存を注入してフィルタを構築する。</summary>
    /// <param name="store">冪等ストア。</param>
    /// <param name="clock">時刻源。</param>
    /// <param name="requestContext">現在のテナント・操作者コンテキスト。</param>
    public IdempotencyFilter(IIdempotencyStore store, IClock clock, RequestContext requestContext)
    {
        _store = store;
        _clock = clock;
        _requestContext = requestContext;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            context.Result = new ObjectResult(
                new ApiError("idempotency_key_required", "Idempotency-Key ヘッダは必須です。"))
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
            return;
        }

        string scopedKey = $"{_requestContext.CurrentTenantId}:{keyValues}";
        string requestHash = ComputeRequestHash(context.ActionArguments);
        DateTimeOffset now = _clock.Now;

        IdempotencyRecord? existing = _store.TryGet(scopedKey, now);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                context.Result = new ObjectResult(
                    new ApiError("idempotency_key_conflict", "同一の Idempotency-Key で異なるリクエストが送信されました。"))
                {
                    StatusCode = StatusCodes.Status409Conflict,
                };
                return;
            }

            context.Result = Replay(existing);
            return;
        }

        ActionExecutedContext executed = await next();

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            return;
        }

        (int statusCode, string? body) = Capture(executed.Result);
        if (statusCode < StatusCodes.Status500InternalServerError)
        {
            _store.Save(scopedKey, new IdempotencyRecord(requestHash, statusCode, body, now));
        }
    }

    private static string ComputeRequestHash(IDictionary<string, object?> actionArguments)
    {
        // CancellationToken 等の非シリアライズ対象を除外し、バインド済み引数のみでハッシュする。
        var hashable = actionArguments
            .Where(entry => entry.Value is not CancellationToken)
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        string json = JsonSerializer.Serialize(hashable);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private static (int StatusCode, string? Body) Capture(IActionResult? result)
        => result switch
        {
            ObjectResult objectResult => (
                objectResult.StatusCode ?? StatusCodes.Status200OK,
                objectResult.Value is null ? null : JsonSerializer.Serialize(objectResult.Value, ResponseJson)),
            StatusCodeResult statusCodeResult => (statusCodeResult.StatusCode, null),
            _ => (StatusCodes.Status200OK, null),
        };

    private static IActionResult Replay(IdempotencyRecord record)
        => record.Body is null
            ? new StatusCodeResult(record.StatusCode)
            : new ContentResult
            {
                StatusCode = record.StatusCode,
                Content = record.Body,
                ContentType = "application/json",
            };
}
