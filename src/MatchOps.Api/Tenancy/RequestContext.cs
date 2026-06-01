// -----------------------------------------------------------------------------
// <copyright file="RequestContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 1 リクエストのテナント・操作者を保持するスコープ付きコンテキスト。
// ITenantContext を実装し、DbContext の Global Query Filter にテナントを供給する（ADR-0006）。
// 値は TenantResolutionMiddleware（認証スタブ）が設定する。未設定時はテナント未解決として扱う。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Tenancy;
using MatchOps.Domain.Common;

namespace MatchOps.Api.Tenancy;

/// <summary>リクエストスコープのテナント・操作者コンテキスト。</summary>
public sealed class RequestContext : ITenantContext
{
    /// <inheritdoc />
    public TenantId? CurrentTenantId { get; private set; }

    /// <summary>現在の操作者識別子（承認者として監査に用いる）。未解決時は <c>null</c>。</summary>
    public string? UserId { get; private set; }

    /// <summary>解決済みのテナント・操作者を設定する（認証スタブから呼ぶ）。</summary>
    /// <param name="tenantId">解決したテナント。</param>
    /// <param name="userId">解決した操作者識別子（任意）。</param>
    public void Resolve(TenantId tenantId, string? userId)
    {
        CurrentTenantId = tenantId;
        UserId = userId;
    }
}
