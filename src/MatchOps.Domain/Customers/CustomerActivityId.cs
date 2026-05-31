// -----------------------------------------------------------------------------
// <copyright file="CustomerActivityId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客行動履歴 (CustomerActivity) の一意識別子（強い型）。Customers モジュール固有。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Customers;

/// <summary>顧客行動履歴 (CustomerActivity) の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct CustomerActivityId(Guid Value)
{
    /// <summary>新しい行動履歴 ID を生成する。</summary>
    /// <returns>一意な <see cref="CustomerActivityId"/>。</returns>
    public static CustomerActivityId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
