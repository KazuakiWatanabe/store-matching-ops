// -----------------------------------------------------------------------------
// <copyright file="OfferId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 提案内容 (Offer：メニュー・クーポン等) の一意識別子（強い型）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>提案内容 (Offer) の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct OfferId(Guid Value)
{
    /// <summary>新しい Offer ID を生成する。</summary>
    /// <returns>一意な <see cref="OfferId"/>。</returns>
    public static OfferId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
