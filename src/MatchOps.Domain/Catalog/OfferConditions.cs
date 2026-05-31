// -----------------------------------------------------------------------------
// <copyright file="OfferConditions.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Offer の適用条件（曜日・対象セグメント）を保持する不変の値オブジェクト。
// 空集合は「制限なし（全曜日／全セグメント）」を意味する。
// 対象セグメントは Customers のセグメント方針に合わせたタグ（文字列）で表す。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Catalog;

/// <summary>Offer の適用条件を保持する不変の値オブジェクト。</summary>
public sealed class OfferConditions
{
    private OfferConditions(IReadOnlySet<DayOfWeek> applicableDays, IReadOnlySet<string> targetSegments)
    {
        ApplicableDays = applicableDays;
        TargetSegments = targetSegments;
    }

    /// <summary>制限なし（全曜日・全セグメント）の条件。</summary>
    public static OfferConditions Unrestricted { get; } =
        new(new HashSet<DayOfWeek>(), new HashSet<string>(StringComparer.Ordinal));

    /// <summary>適用曜日（空なら全曜日）。</summary>
    public IReadOnlySet<DayOfWeek> ApplicableDays { get; }

    /// <summary>対象セグメントタグ（空なら全セグメント）。</summary>
    public IReadOnlySet<string> TargetSegments { get; }

    /// <summary>
    /// 適用条件を生成する。いずれも <c>null</c> または空なら「制限なし」として扱う。
    /// </summary>
    /// <param name="applicableDays">適用曜日（任意）。</param>
    /// <param name="targetSegments">対象セグメントタグ（任意・空白不可）。</param>
    /// <returns>生成された <see cref="OfferConditions"/>。</returns>
    /// <exception cref="DomainException">セグメントタグが空白の場合。</exception>
    public static OfferConditions Create(
        IEnumerable<DayOfWeek>? applicableDays = null,
        IEnumerable<string>? targetSegments = null)
    {
        var days = applicableDays is null ? new HashSet<DayOfWeek>() : new HashSet<DayOfWeek>(applicableDays);
        var segments = new HashSet<string>(StringComparer.Ordinal);

        if (targetSegments is not null)
        {
            foreach (string segment in targetSegments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    throw new DomainException("対象セグメントタグが空です。");
                }

                segments.Add(segment.Trim());
            }
        }

        return new OfferConditions(days, segments);
    }

    /// <summary>指定日（曜日）に適用可能かを返す。</summary>
    /// <param name="date">対象日。</param>
    /// <returns>適用可能なら <c>true</c>。</returns>
    public bool AppliesOn(DateOnly date) =>
        ApplicableDays.Count == 0 || ApplicableDays.Contains(date.DayOfWeek);

    /// <summary>指定セグメントを対象とするかを返す。</summary>
    /// <param name="segment">セグメントタグ。</param>
    /// <returns>対象なら <c>true</c>。</returns>
    public bool TargetsSegment(string segment) =>
        TargetSegments.Count == 0 || TargetSegments.Contains(segment);
}
