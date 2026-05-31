// -----------------------------------------------------------------------------
// <copyright file="ScoreBreakdown.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// スコアの内訳（項目名→寄与値）を保持する不変の値オブジェクト。
// 「なぜその顧客に提案するか」を常に説明可能にするための説明性の中核（ADR-0003）。
// 合計 (Total) は寄与値の総和から導出され、常に MatchScore と整合する。
// 各寄与値 = 正規化された素点 × 再正規化後の重み（0〜1）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>
/// スコアの内訳（項目名→寄与値）と合計スコアを保持する不変の値オブジェクト。
/// </summary>
public sealed class ScoreBreakdown
{
    /// <summary>浮動小数の累積誤差を吸収するための許容誤差。</summary>
    private const double Epsilon = 1e-9;

    private readonly IReadOnlyDictionary<string, double> _contributions;

    private ScoreBreakdown(IReadOnlyDictionary<string, double> contributions, MatchScore total)
    {
        _contributions = contributions;
        Total = total;
    }

    /// <summary>項目名 → 寄与値（0〜1）の内訳。</summary>
    public IReadOnlyDictionary<string, double> Contributions => _contributions;

    /// <summary>寄与値の総和から導出した合計スコア。常に内訳と整合する。</summary>
    public MatchScore Total { get; }

    /// <summary>
    /// 項目名→寄与値の内訳を検証して <see cref="ScoreBreakdown"/> を生成する。
    /// 合計は寄与値の総和として算出する。空の内訳は合計 0 として扱う。
    /// </summary>
    /// <param name="contributions">項目名 → 寄与値（各 0〜1）。</param>
    /// <returns>生成された <see cref="ScoreBreakdown"/>。</returns>
    /// <exception cref="DomainException">
    /// 項目名が空、寄与値が NaN・無限大・範囲外、または総和が 0〜1 を超える場合。
    /// </exception>
    public static ScoreBreakdown From(IReadOnlyDictionary<string, double> contributions)
    {
        if (contributions is null)
        {
            throw new DomainException("スコア内訳が指定されていません。");
        }

        var snapshot = new Dictionary<string, double>(contributions.Count, StringComparer.Ordinal);
        double sum = 0d;

        foreach (KeyValuePair<string, double> entry in contributions)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new DomainException("スコア項目名が空です。");
            }

            double value = entry.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new DomainException($"スコア寄与値が有効な数値ではありません: 項目 '{entry.Key}'");
            }

            if (value < 0d || value > 1d)
            {
                throw new DomainException($"スコア寄与値は 0〜1 の範囲で指定してください: 項目 '{entry.Key}' = {value}");
            }

            snapshot[entry.Key] = value;
            sum += value;
        }

        // 各寄与値は 0〜1 に検証済みのため総和は常に 0 以上。
        // 浮動小数の累積誤差で僅かに 1 を超えた場合のみ 1 にクランプする。
        if (sum > 1d && sum <= 1d + Epsilon)
        {
            sum = 1d;
        }

        return new ScoreBreakdown(snapshot, MatchScore.From(sum));
    }
}
