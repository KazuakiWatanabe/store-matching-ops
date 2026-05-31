// -----------------------------------------------------------------------------
// <copyright file="MatchScore.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 来店可能性スコアを表す値オブジェクト。0〜1 に正規化された説明可能なスコア。
// Phase 0/1 はルールベース + 重み付け（ADR-0003）。欠損項目は重みから除外し、
// 残り項目で動的に再正規化するため、最終スコアは常に 0〜1 に収まる。
// 関連: ADR-0003 (ルールベース・スコアリング), ScoreBreakdown（内訳）
// </summary>
// -----------------------------------------------------------------------------

using System.Globalization;

namespace MatchOps.Domain.Common;

/// <summary>
/// 0〜1 に正規化された来店可能性スコアを表す不変の値オブジェクト。
/// </summary>
public readonly record struct MatchScore
{
    private MatchScore(double value) => Value = value;

    /// <summary>スコア 0（最小値）。</summary>
    public static MatchScore Zero { get; } = new(0d);

    /// <summary>0〜1 に正規化されたスコア値。</summary>
    public double Value { get; }

    /// <summary>
    /// スコア値を検証して <see cref="MatchScore"/> を生成する。
    /// </summary>
    /// <param name="value">0〜1 の範囲のスコア値。</param>
    /// <returns>生成された <see cref="MatchScore"/>。</returns>
    /// <exception cref="DomainException">
    /// 値が NaN・無限大、または 0〜1 の範囲外の場合。
    /// </exception>
    public static MatchScore From(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new DomainException($"スコアが有効な数値ではありません: {value}");
        }

        if (value < 0d || value > 1d)
        {
            throw new DomainException($"スコアは 0〜1 の範囲で指定してください: {value}");
        }

        return new MatchScore(value);
    }

    /// <summary>不変カルチャで小数 4 桁までの文字列に整形する。</summary>
    /// <returns>スコアの文字列表現。</returns>
    public override string ToString() => Value.ToString("0.####", CultureInfo.InvariantCulture);
}
