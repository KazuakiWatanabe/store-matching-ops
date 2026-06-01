// -----------------------------------------------------------------------------
// <copyright file="TenantResolutionMiddleware.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 認証スタブ兼テナント解決ミドルウェア（Phase 0）。
// 業務 API（/api 配下）では X-Tenant-Id ヘッダからテナントを解決し RequestContext に設定する（ADR-0006）。
// テナント未指定・不正なら 401。X-User-Id（操作者）は任意で解決する。
// 本実装は Phase 1 の本認証（OIDC/JWT・ロール）に置き換える前提のスタブである。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Api.Tenancy;

/// <summary>ヘッダからテナント・操作者を解決する認証スタブミドルウェア。</summary>
public sealed class TenantResolutionMiddleware
{
    /// <summary>テナント識別子を渡すヘッダ名（認証スタブ）。</summary>
    public const string TenantHeader = "X-Tenant-Id";

    /// <summary>操作者識別子を渡すヘッダ名（認証スタブ）。</summary>
    public const string UserHeader = "X-User-Id";

    private const string ApiPathPrefix = "/api";

    private readonly RequestDelegate _next;

    /// <summary>ミドルウェアを構築する。</summary>
    /// <param name="next">次のミドルウェア。</param>
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    /// <summary>リクエストを処理し、業務 API ではテナントを解決する。</summary>
    /// <param name="context">HTTP コンテキスト。</param>
    /// <param name="requestContext">解決先のリクエストコンテキスト。</param>
    /// <returns>処理を表すタスク。</returns>
    public async Task InvokeAsync(HttpContext context, RequestContext requestContext)
    {
        // 業務 API 以外（/health, /openapi, /scalar 等）はテナント解決を要求しない。
        if (!context.Request.Path.StartsWithSegments(ApiPathPrefix))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(TenantHeader, out var tenantValues)
            || !Guid.TryParse(tenantValues.ToString(), out Guid tenantGuid))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new { errorCode = "tenant_required", errorMessage = "テナントが特定できません。" });
            return;
        }

        string? userId = context.Request.Headers.TryGetValue(UserHeader, out var userValues)
            ? userValues.ToString()
            : null;

        requestContext.Resolve(new TenantId(tenantGuid), string.IsNullOrWhiteSpace(userId) ? null : userId);

        await _next(context);
    }
}
