// -----------------------------------------------------------------------------
// <copyright file="OfferOption.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 候補に対して提案しうる Offer の選択肢（Matching ローカルの入力抽象）。
// 有効性・適用可否・値引き上限内かは Catalog の Offer/DiscountCap から Application が判定して写像する（CLAUDE.md §4.1）。
// Matching はこれらの真偽値で利用可否を判定する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>候補に提案しうる Offer の選択肢。</summary>
/// <param name="OfferId">Offer ID。</param>
/// <param name="IsActive">Offer が有効か。</param>
/// <param name="AppliesToSlot">対象枠の日付・条件に適合するか。</param>
/// <param name="DiscountWithinCap">提案する値引きが Offer の上限内か（値引きなしの場合も true）。</param>
public sealed record OfferOption(OfferId OfferId, bool IsActive, bool AppliesToSlot, bool DiscountWithinCap)
{
    /// <summary>この選択肢が提案候補として利用可能か（有効・適用可・上限内のすべてを満たす）。</summary>
    public bool IsUsable => IsActive && AppliesToSlot && DiscountWithinCap;
}
