// -----------------------------------------------------------------------------
// <copyright file="ScoringFactors.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// v0 スコアリングの項目名（ScoreInputs と ScoringPolicy で共有するキー）。
// 文字列キーで段階的スコアリング（欠損項目の動的再正規化）を実現する（ADR-0003）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Matching;

/// <summary>v0 スコアリングの項目名（キー）。</summary>
public static class ScoringFactors
{
    /// <summary>休眠日数スコア（最終来店からの経過に基づく）。</summary>
    public const string Dormancy = "dormancy";

    /// <summary>来店周期との乖離スコア。</summary>
    public const string VisitCycleDeviation = "visit_cycle_deviation";

    /// <summary>空き枠の曜日・時間帯一致スコア。</summary>
    public const string SlotDayTimeMatch = "slot_day_time_match";
}
