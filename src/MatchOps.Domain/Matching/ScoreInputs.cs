// -----------------------------------------------------------------------------
// <copyright file="ScoreInputs.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客・空き枠から計算済みの特徴量（項目名→0〜1 の正規化値）を受け取る入力。
// 欠損項目は単に含めない（ScoringPolicy が残り項目で再正規化する）。Domain 内で完結する。
// 特徴量の抽出・正規化自体は Application/Infrastructure が行い、ここには結果のみ渡る。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>スコア計算の入力特徴量（項目名→0〜1）を保持する不変の入力。</summary>
public sealed class ScoreInputs
{
    private ScoreInputs(IReadOnlyDictionary<string, double> values) => Values = values;

    /// <summary>項目名→正規化値（0〜1）。欠損項目は含まれない。</summary>
    public IReadOnlyDictionary<string, double> Values { get; }

    /// <summary>
    /// 項目名→値の辞書から入力を生成する。各値は 0〜1 でなければならない。
    /// </summary>
    /// <param name="values">項目名→正規化値（0〜1）。</param>
    /// <returns>生成された <see cref="ScoreInputs"/>。</returns>
    /// <exception cref="DomainException">項目名が空、または値が 0〜1 の範囲外・非数の場合。</exception>
    public static ScoreInputs FromValues(IReadOnlyDictionary<string, double> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var snapshot = new Dictionary<string, double>(values.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, double> entry in values)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new DomainException("スコア項目名が空です。");
            }

            double value = entry.Value;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            {
                throw new DomainException($"スコア特徴量は 0〜1 で指定してください: 項目 '{entry.Key}' = {value}");
            }

            snapshot[entry.Key] = value;
        }

        return new ScoreInputs(snapshot);
    }

    /// <summary>
    /// v0 の 3 特徴量から入力を生成する。<c>null</c> の項目は欠損として除外する。
    /// </summary>
    /// <param name="dormancy">休眠日数スコア（0〜1。欠損なら <c>null</c>）。</param>
    /// <param name="visitCycleDeviation">来店周期との乖離スコア（0〜1。欠損なら <c>null</c>）。</param>
    /// <param name="slotDayTimeMatch">空き枠の曜日・時間帯一致スコア（0〜1。欠損なら <c>null</c>）。</param>
    /// <returns>生成された <see cref="ScoreInputs"/>。</returns>
    public static ScoreInputs ForV0(double? dormancy, double? visitCycleDeviation, double? slotDayTimeMatch)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        if (dormancy is { } d)
        {
            values[ScoringFactors.Dormancy] = d;
        }

        if (visitCycleDeviation is { } c)
        {
            values[ScoringFactors.VisitCycleDeviation] = c;
        }

        if (slotDayTimeMatch is { } s)
        {
            values[ScoringFactors.SlotDayTimeMatch] = s;
        }

        return FromValues(values);
    }
}
