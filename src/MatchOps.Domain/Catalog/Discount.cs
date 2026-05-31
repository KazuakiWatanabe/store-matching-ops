// -----------------------------------------------------------------------------
// <copyright file="Discount.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 提案される値引きを表す値オブジェクト。金額または率のいずれか。
// 値引き上限 (DiscountCap) と突き合わせて検証する（設計 §6.3, ADR-0010）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Catalog;

/// <summary>提案される値引きを表す不変の値オブジェクト。</summary>
public readonly record struct Discount
{
    private Discount(DiscountKind kind, Money amount, decimal rate)
    {
        Kind = kind;
        Amount = amount;
        Rate = rate;
    }

    /// <summary>値引きの表現方法。</summary>
    public DiscountKind Kind { get; }

    /// <summary>値引き金額（<see cref="Kind"/> が <see cref="DiscountKind.Amount"/> のときのみ有効）。</summary>
    public Money Amount { get; }

    /// <summary>値引き率 0〜1（<see cref="Kind"/> が <see cref="DiscountKind.Rate"/> のときのみ有効）。</summary>
    public decimal Rate { get; }

    /// <summary>金額による値引きを生成する。</summary>
    /// <param name="amount">値引き金額（負不可）。</param>
    /// <returns>金額ベースの <see cref="Discount"/>。</returns>
    /// <exception cref="DomainException">金額が負の場合。</exception>
    public static Discount OfAmount(Money amount)
    {
        if (amount.Amount < 0m)
        {
            throw new DomainException("値引き金額は負にできません。");
        }

        return new Discount(DiscountKind.Amount, amount, 0m);
    }

    /// <summary>率による値引きを生成する。</summary>
    /// <param name="rate">値引き率（0 より大きく 1 以下）。</param>
    /// <returns>率ベースの <see cref="Discount"/>。</returns>
    /// <exception cref="DomainException">率が 0 以下または 1 超の場合。</exception>
    public static Discount OfRate(decimal rate)
    {
        if (rate is <= 0m or > 1m)
        {
            throw new DomainException("値引き率は 0 より大きく 1 以下で指定してください。");
        }

        return new Discount(DiscountKind.Rate, default, rate);
    }
}
