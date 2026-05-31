// -----------------------------------------------------------------------------
// <copyright file="StoreId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 店舗の一意識別子（強い型）。テナント配下の店舗を一意に表す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>店舗の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct StoreId(Guid Value)
{
    /// <summary>新しい店舗 ID を生成する。</summary>
    /// <returns>一意な <see cref="StoreId"/>。</returns>
    public static StoreId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
