// -----------------------------------------------------------------------------
// <copyright file="MatchingOptions.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// マッチングのスコアリング重み・通知頻度上限の設定（CLAUDE.md §4.3：重みはコード直書きせず設定から注入）。
// 重み未設定時は ScoringPolicy の v0 既定を用いる。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Infrastructure.Matching;

/// <summary>マッチングのスコアリング・頻度設定。</summary>
public sealed class MatchingOptions
{
    /// <summary>設定セクション名。</summary>
    public const string SectionName = "Matching";

    /// <summary>スコアリング項目名→重み（空の場合は v0 既定を使用）。</summary>
    public Dictionary<string, double> ScoringWeights { get; init; } = new(StringComparer.Ordinal);

    /// <summary>通知の最小間隔日数（0 で制限なし）。</summary>
    public int NotificationMinIntervalDays { get; init; }
}
