// -----------------------------------------------------------------------------
// <copyright file="DiscountCap.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 値引き上限を表す値オブジェクト。金額上限または率上限のいずれか（ADR-0010）。
// 提案された値引きが上限を超えないことを EnsureWithin で機械的に保証する（CLAUDE.md §4.3）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Catalog;

/// <summary>値引き上限を表す不変の値オブジェクト。</summary>
public readonly record struct DiscountCap
{
    private DiscountCap(DiscountKind kind, Money maxAmount, decimal maxRate)
    {
        Kind = kind;
        MaxAmount = maxAmount;
        MaxRate = maxRate;
    }

    /// <summary>上限の表現方法。</summary>
    public DiscountKind Kind { get; }

    /// <summary>上限金額（<see cref="Kind"/> が <see cref="DiscountKind.Amount"/> のときのみ有効）。</summary>
    public Money MaxAmount { get; }

    /// <summary>上限率 0〜1（<see cref="Kind"/> が <see cref="DiscountKind.Rate"/> のときのみ有効）。</summary>
    public decimal MaxRate { get; }

    /// <summary>金額上限を生成する。</summary>
    /// <param name="maxAmount">上限金額（負不可）。</param>
    /// <returns>金額上限の <see cref="DiscountCap"/>。</returns>
    /// <exception cref="DomainException">上限金額が負の場合。</exception>
    public static DiscountCap Amount(Money maxAmount)
    {
        if (maxAmount.Amount < 0m)
        {
            throw new DomainException("値引き上限額は負にできません。");
        }

        return new DiscountCap(DiscountKind.Amount, maxAmount, 0m);
    }

    /// <summary>率上限を生成する。</summary>
    /// <param name="maxRate">上限率（0 より大きく 1 以下）。</param>
    /// <returns>率上限の <see cref="DiscountCap"/>。</returns>
    /// <exception cref="DomainException">上限率が 0 以下または 1 超の場合。</exception>
    public static DiscountCap Rate(decimal maxRate)
    {
        if (maxRate is <= 0m or > 1m)
        {
            throw new DomainException("値引き上限率は 0 より大きく 1 以下で指定してください。");
        }

        return new DiscountCap(DiscountKind.Rate, default, maxRate);
    }

    /// <summary>
    /// 提案された値引きが上限を超えないことを保証する。種別・通貨が一致しない、
    /// または上限を超える場合は <see cref="DomainException"/> を送出する。
    /// </summary>
    /// <param name="proposed">提案された値引き。</param>
    /// <exception cref="DomainException">種別不一致・通貨不一致・上限超過の場合。</exception>
    public void EnsureWithin(Discount proposed)
    {
        if (proposed.Kind != Kind)
        {
            throw new DomainException($"値引きの種別が上限と一致しません: 上限={Kind}, 提案={proposed.Kind}");
        }

        if (Kind == DiscountKind.Amount)
        {
            if (proposed.Amount.Currency != MaxAmount.Currency)
            {
                throw new DomainException("値引きの通貨が上限と一致しません。");
            }

            if (proposed.Amount.Amount > MaxAmount.Amount)
            {
                throw new DomainException("提案された値引き金額が上限を超えています。");
            }
        }
        else if (proposed.Rate > MaxRate)
        {
            throw new DomainException("提案された値引き率が上限を超えています。");
        }
    }
}
