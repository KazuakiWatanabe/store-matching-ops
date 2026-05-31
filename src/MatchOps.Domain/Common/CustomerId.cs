// -----------------------------------------------------------------------------
// <copyright file="CustomerId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客の一意識別子（強い型）。識別子は最小化し、連絡先等の PII とは分離する。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>顧客の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct CustomerId(Guid Value)
{
    /// <summary>新しい顧客 ID を生成する。</summary>
    /// <returns>一意な <see cref="CustomerId"/>。</returns>
    public static CustomerId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
