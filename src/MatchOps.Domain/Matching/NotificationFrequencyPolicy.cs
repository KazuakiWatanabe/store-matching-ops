// -----------------------------------------------------------------------------
// <copyright file="NotificationFrequencyPolicy.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 通知頻度の上限ポリシー。最終通知日からの最小間隔（日数）を表す値オブジェクト。
// 配信制御（通知疲れ防止）の一部（ADR-0010）。最終通知日は日付で受け取る（CLAUDE.md §10.4）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>通知頻度上限（最小間隔日数）を表す不変の値オブジェクト。</summary>
public readonly record struct NotificationFrequencyPolicy
{
    private NotificationFrequencyPolicy(int minIntervalDays) => MinIntervalDays = minIntervalDays;

    /// <summary>頻度制限なし（最小間隔 0 日）。</summary>
    public static NotificationFrequencyPolicy Unlimited => new(0);

    /// <summary>最終通知からの最小間隔（日数）。</summary>
    public int MinIntervalDays { get; }

    /// <summary>最小間隔（日数）を指定してポリシーを生成する。</summary>
    /// <param name="minIntervalDays">最小間隔日数（0 以上）。</param>
    /// <returns>生成された <see cref="NotificationFrequencyPolicy"/>。</returns>
    /// <exception cref="DomainException">日数が負の場合。</exception>
    public static NotificationFrequencyPolicy OfMinIntervalDays(int minIntervalDays)
    {
        if (minIntervalDays < 0)
        {
            throw new DomainException("通知の最小間隔日数は 0 以上で指定してください。");
        }

        return new NotificationFrequencyPolicy(minIntervalDays);
    }

    /// <summary>
    /// 指定日に通知が許可されるかを返す。未通知（<paramref name="lastNotifiedOn"/> が <c>null</c>）なら常に許可。
    /// </summary>
    /// <param name="lastNotifiedOn">最終通知日（未通知なら <c>null</c>）。</param>
    /// <param name="today">基準日。</param>
    /// <returns>許可されるなら <c>true</c>。</returns>
    public bool Allows(DateOnly? lastNotifiedOn, DateOnly today)
        => lastNotifiedOn is not { } last || today.DayNumber - last.DayNumber >= MinIntervalDays;
}
