// -----------------------------------------------------------------------------
// <copyright file="ContactHash.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 連絡先（電話・メール）のハッシュ値を表す値オブジェクト。
// 平文の連絡先 (PII) を Domain に持ち込ませないための境界（ADR-0005, 設計 §9）。
// SHA-256 を表す 64 桁の 16 進文字列のみ受理し、平文を渡すと拒否する。
// 例外メッセージには入力値を含めない（平文 PII のログ流出防止）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Customers;

/// <summary>連絡先のハッシュ値（SHA-256 の 64 桁 16 進）を表す不変の値オブジェクト。</summary>
public readonly record struct ContactHash
{
    /// <summary>SHA-256 を 16 進表現したときの文字数。</summary>
    private const int Sha256HexLength = 64;

    private ContactHash(string value) => Value = value;

    /// <summary>小文字に正規化された 16 進ハッシュ文字列。</summary>
    public string Value { get; }

    /// <summary>
    /// ハッシュ文字列を検証して <see cref="ContactHash"/> を生成する。
    /// SHA-256 を表す 64 桁の 16 進文字列のみ受理する（平文連絡先は拒否）。
    /// </summary>
    /// <param name="hash">SHA-256 を 16 進表現した 64 桁の文字列。</param>
    /// <returns>生成された <see cref="ContactHash"/>。</returns>
    /// <exception cref="DomainException">
    /// 64 桁の 16 進文字列でない場合（平文連絡先を渡した場合を含む）。
    /// PII 漏えい防止のため、例外メッセージに入力値は含めない。
    /// </exception>
    public static ContactHash From(string hash)
    {
        if (!IsSha256Hex(hash))
        {
            throw new DomainException(
                "連絡先はハッシュ値（SHA-256 を表す 64 桁の 16 進文字列）で指定してください。平文を渡してはいけません。");
        }

        return new ContactHash(hash.ToLowerInvariant());
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    private static bool IsSha256Hex(string? value)
        => value is { Length: Sha256HexLength } && value.All(Uri.IsHexDigit);
}
