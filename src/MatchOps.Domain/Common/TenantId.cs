// -----------------------------------------------------------------------------
// <copyright file="TenantId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// テナントの一意識別子（強い型）。すべての業務データはこの ID でスコープする（ADR-0006）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>テナントの一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct TenantId(Guid Value)
{
    /// <summary>新しいテナント ID を生成する。</summary>
    /// <returns>一意な <see cref="TenantId"/>。</returns>
    public static TenantId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
