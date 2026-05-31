// -----------------------------------------------------------------------------
// <copyright file="ScoringPolicy.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// v0 ルールベース・スコアリング（ADR-0003）。項目名→重みを保持し、
// 入力にある項目のみで重みを動的に再正規化して 0〜1 の MatchScore を算出する（段階的スコアリング）。
// 重みは設定から注入する（コードに直書きしない）。既定値は ScoringPolicy.CreateV0Default。
// 機械学習は用いない（説明可能なルール＋重みのみ）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>項目名→重みに基づく v0 スコアリングポリシー。</summary>
public sealed class ScoringPolicy
{
    private ScoringPolicy(IReadOnlyDictionary<string, double> normalizedWeights) => Weights = normalizedWeights;

    /// <summary>正規化済みの項目名→重み（合計 1）。</summary>
    public IReadOnlyDictionary<string, double> Weights { get; }

    /// <summary>
    /// 重みを検証・正規化して（合計 1 に）ポリシーを生成する。重みは設定から注入する。
    /// </summary>
    /// <param name="weights">項目名→重み（各 &gt; 0。相対値でよい）。</param>
    /// <returns>生成された <see cref="ScoringPolicy"/>。</returns>
    /// <exception cref="DomainException">重みが空、項目名が空、または重みが正でない場合。</exception>
    public static ScoringPolicy Create(IReadOnlyDictionary<string, double> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0)
        {
            throw new DomainException("重みが 1 件も指定されていません。");
        }

        double total = 0d;
        foreach (KeyValuePair<string, double> entry in weights)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new DomainException("重みの項目名が空です。");
            }

            if (double.IsNaN(entry.Value) || double.IsInfinity(entry.Value) || entry.Value <= 0d)
            {
                throw new DomainException($"重みは正の数で指定してください: 項目 '{entry.Key}' = {entry.Value}");
            }

            total += entry.Value;
        }

        var normalized = new Dictionary<string, double>(weights.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, double> entry in weights)
        {
            normalized[entry.Key] = entry.Value / total;
        }

        return new ScoringPolicy(normalized);
    }

    /// <summary>
    /// ADR-0003 の既定重み（休眠 0.40 / 来店周期乖離 0.35 / 曜日時間帯一致 0.25）でポリシーを生成する。
    /// 実運用では設定からの注入（<see cref="Create"/>）で上書きする。
    /// </summary>
    /// <returns>既定重みの <see cref="ScoringPolicy"/>。</returns>
    public static ScoringPolicy CreateV0Default()
        => Create(new Dictionary<string, double>(StringComparer.Ordinal)
        {
            [ScoringFactors.Dormancy] = 0.40d,
            [ScoringFactors.VisitCycleDeviation] = 0.35d,
            [ScoringFactors.SlotDayTimeMatch] = 0.25d,
        });

    /// <summary>
    /// 入力特徴量からスコア内訳を算出する。入力に存在する項目のみを対象に重みを再正規化する。
    /// 対象項目が 1 つもない場合は合計 0（<see cref="MatchScore.Zero"/>）の空内訳を返す。
    /// </summary>
    /// <param name="inputs">スコア入力特徴量。</param>
    /// <returns>各項目の寄与と合計スコアを保持する <see cref="ScoreBreakdown"/>。</returns>
    public ScoreBreakdown Score(ScoreInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        double availableWeight = 0d;
        foreach (KeyValuePair<string, double> weight in Weights)
        {
            if (inputs.Values.ContainsKey(weight.Key))
            {
                availableWeight += weight.Value;
            }
        }

        var contributions = new Dictionary<string, double>(StringComparer.Ordinal);
        if (availableWeight > 0d)
        {
            foreach (KeyValuePair<string, double> weight in Weights)
            {
                if (inputs.Values.TryGetValue(weight.Key, out double value))
                {
                    contributions[weight.Key] = value * (weight.Value / availableWeight);
                }
            }
        }

        return ScoreBreakdown.From(contributions);
    }
}
