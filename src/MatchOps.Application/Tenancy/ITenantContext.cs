// -----------------------------------------------------------------------------
// <copyright file="ITenantContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 現在のテナントを解決する抽象。データアクセスのテナントスコープ（Global Query Filter）に用いる（ADR-0006）。
// Api は認証コンテキストから、Worker はジョブのテナントから解決する。
// 解決できない場合は null を返し、その場合データアクセスは「何も返さない」安全側に倒す。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Tenancy;

/// <summary>現在のテナントを提供する抽象。</summary>
public interface ITenantContext
{
    /// <summary>現在のテナント ID。解決できない場合は <c>null</c>。</summary>
    TenantId? CurrentTenantId { get; }
}
