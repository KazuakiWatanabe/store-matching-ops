// -----------------------------------------------------------------------------
// <copyright file="ResourceId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// リソース (席・スタッフ等) の一意識別子（強い型）。Scheduling モジュール固有。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Scheduling;

/// <summary>リソースの一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct ResourceId(Guid Value)
{
    /// <summary>新しいリソース ID を生成する。</summary>
    /// <returns>一意な <see cref="ResourceId"/>。</returns>
    public static ResourceId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
