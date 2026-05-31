// -----------------------------------------------------------------------------
// <copyright file="DiscountKind.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 値引きの表現方法。金額ベースか率ベースか（設計 §6.3, ADR-0010）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Catalog;

/// <summary>値引きの表現方法。</summary>
public enum DiscountKind
{
    /// <summary>金額による値引き（例: 500 円引き）。</summary>
    Amount = 0,

    /// <summary>率による値引き（例: 20% 引き）。</summary>
    Rate = 1,
}
