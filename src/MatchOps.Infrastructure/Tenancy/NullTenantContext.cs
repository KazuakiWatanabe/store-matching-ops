// -----------------------------------------------------------------------------
// <copyright file="NullTenantContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// テナント未解決の既定 ITenantContext（背景処理＝Worker 用）。CurrentTenantId は常に null。
// Api は認証コンテキストから解決する RequestContext を登録するため、AddInfrastructure は TryAdd で本実装を既定登録し、
// 既存登録（Api の RequestContext）を上書きしない。背景ジョブはテナント横断のため IgnoreQueryFilters で処理する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Tenancy;
using MatchOps.Domain.Common;

namespace MatchOps.Infrastructure.Tenancy;

/// <summary>テナント未解決の既定テナントコンテキスト（背景処理用）。</summary>
public sealed class NullTenantContext : ITenantContext
{
    /// <inheritdoc />
    public TenantId? CurrentTenantId => null;
}
