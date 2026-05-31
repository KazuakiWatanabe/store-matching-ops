// -----------------------------------------------------------------------------
// <copyright file="Money.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 金額を表す値オブジェクト。金額（decimal）と ISO 4217 通貨コード（英大文字3文字）を保持する。
// JPY は小数を持てない。異なる通貨どうしの演算は DomainException を送出する。
// 値引き上限・客単価などの金額計算に用いる（負値は値引き・返金として許容）。
// </summary>
// -----------------------------------------------------------------------------

using System.Globalization;

namespace MatchOps.Domain.Common;

/// <summary>
/// 金額と通貨を保持する不変の値オブジェクト。生成は Factory（<see cref="Of"/> / <see cref="Jpy"/>）経由。
/// </summary>
public readonly record struct Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>金額。負値は値引き・返金として許容する。</summary>
    public decimal Amount { get; }

    /// <summary>ISO 4217 通貨コード（英大文字3文字）。</summary>
    public string Currency { get; }

    /// <summary>
    /// 金額と通貨コードを検証して <see cref="Money"/> を生成する。
    /// </summary>
    /// <param name="amount">金額。JPY の場合は小数を持てない。</param>
    /// <param name="currency">ISO 4217 通貨コード（英大文字3文字）。</param>
    /// <returns>生成された <see cref="Money"/>。</returns>
    /// <exception cref="DomainException">
    /// 通貨コードが英大文字3文字でない、または JPY に小数が含まれる場合。
    /// </exception>
    public static Money Of(decimal amount, string currency)
    {
        if (!IsValidCurrencyCode(currency))
        {
            throw new DomainException($"通貨コードは ISO 4217 の英大文字3文字で指定してください: '{currency}'");
        }

        if (currency == "JPY" && decimal.Truncate(amount) != amount)
        {
            throw new DomainException($"JPY は小数を持てません: {amount}");
        }

        return new Money(amount, currency);
    }

    /// <summary>日本円 (JPY) の <see cref="Money"/> を生成する。</summary>
    /// <param name="amount">金額（小数不可）。</param>
    /// <returns>生成された JPY の <see cref="Money"/>。</returns>
    /// <exception cref="DomainException">金額に小数が含まれる場合。</exception>
    public static Money Jpy(decimal amount) => Of(amount, "JPY");

    /// <summary>同一通貨どうしの金額を加算する。</summary>
    /// <param name="left">左オペランド。</param>
    /// <param name="right">右オペランド。</param>
    /// <returns>加算結果。</returns>
    /// <exception cref="DomainException">通貨が異なる場合。</exception>
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>同一通貨どうしの金額を減算する。</summary>
    /// <param name="left">左オペランド。</param>
    /// <param name="right">右オペランド。</param>
    /// <returns>減算結果。</returns>
    /// <exception cref="DomainException">通貨が異なる場合。</exception>
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>「金額 通貨」形式の文字列に整形する（不変カルチャ）。</summary>
    /// <returns>金額の文字列表現。</returns>
    public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException($"異なる通貨は演算できません: {left.Currency} と {right.Currency}");
        }
    }

    private static bool IsValidCurrencyCode(string? code)
        => code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);
}
