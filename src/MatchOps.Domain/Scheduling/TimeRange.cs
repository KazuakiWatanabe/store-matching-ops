// -----------------------------------------------------------------------------
// <copyright file="TimeRange.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 時間範囲を表す値オブジェクト。半開区間 [Start, End) として扱う。
// 終了は開始より後でなければならない。重複判定 (Overlaps) を提供する。
// 時刻は IClock を知らず、呼び出し側が DateTimeOffset を渡す（CLAUDE.md §10.4）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Scheduling;

/// <summary>半開区間 [Start, End) の時間範囲を表す不変の値オブジェクト。</summary>
public readonly record struct TimeRange
{
    private TimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    /// <summary>開始日時（含む）。</summary>
    public DateTimeOffset Start { get; }

    /// <summary>終了日時（含まない）。</summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// 開始・終了を検証して <see cref="TimeRange"/> を生成する。
    /// </summary>
    /// <param name="start">開始日時。</param>
    /// <param name="end">終了日時（開始より後）。</param>
    /// <returns>生成された <see cref="TimeRange"/>。</returns>
    /// <exception cref="DomainException">終了が開始以前の場合。</exception>
    public static TimeRange Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new DomainException("時間範囲の終了は開始より後である必要があります。");
        }

        return new TimeRange(start, end);
    }

    /// <summary>
    /// 他の時間範囲と重複するかを返す（半開区間。隣接は重複しない）。
    /// </summary>
    /// <param name="other">比較対象。</param>
    /// <returns>重複する場合は <c>true</c>。</returns>
    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;

    /// <summary>ISO 8601 ラウンドトリップ形式（不変）で「開始/終了」を返す。</summary>
    /// <returns>時間範囲の文字列表現。</returns>
    public override string ToString() => $"{Start:o}/{End:o}";
}
