// -----------------------------------------------------------------------------
// <copyright file="ValueObjectConverters.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ドメイン値オブジェクトの永続化用 ValueConverter / ValueComparer 群。
// Domain は private ctor + factory 検証を持つため、変換は「列の素朴な表現 ↔ factory 再構築」で行う。
// 複合値（時間範囲・カテゴリ集合・値引き上限・適用条件）は jsonb 列に保存する（設計 §8）。
// </summary>
// -----------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;
using MatchOps.Domain.Scheduling;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MatchOps.Infrastructure.Persistence.Conversions;

/// <summary>値オブジェクトの ValueConverter / ValueComparer を提供する。</summary>
public static class ValueObjectConverters
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    /// <summary>連絡先ハッシュ ↔ 文字列。</summary>
    public static ValueConverter<ContactHash, string> ContactHash { get; } =
        new(hash => hash.Value, value => Domain.Customers.ContactHash.From(value));

    /// <summary>金額 ↔ 「金額 通貨」文字列（不変カルチャ）。</summary>
    public static ValueConverter<Money, string> Money { get; } =
        new(money => money.ToString(), value => ParseMoney(value));

    /// <summary>時間範囲 ↔ jsonb。</summary>
    public static ValueConverter<TimeRange, string> TimeRange { get; } =
        new(
            range => JsonSerializer.Serialize(new TimeRangeJson(range.Start, range.End), Json),
            value => ToTimeRange(JsonSerializer.Deserialize<TimeRangeJson>(value, Json)!));

    /// <summary>対応 Offer 種別タグ集合 ↔ jsonb 配列。</summary>
    public static ValueConverter<IReadOnlySet<string>, string> OfferCategories { get; } =
        new(
            set => JsonSerializer.Serialize(set, Json),
            value => JsonSerializer.Deserialize<HashSet<string>>(value, Json) ?? new HashSet<string>(StringComparer.Ordinal));

    /// <summary>タグ集合の変更追跡用比較子。</summary>
    public static ValueComparer<IReadOnlySet<string>> OfferCategoriesComparer { get; } =
        new(
            (left, right) => left!.SetEquals(right!),
            set => set.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
            set => new HashSet<string>(set, StringComparer.Ordinal));

    /// <summary>値引き上限 ↔ jsonb。</summary>
    public static ValueConverter<DiscountCap, string> DiscountCap { get; } =
        new(
            cap => JsonSerializer.Serialize(ToJson(cap), Json),
            value => ToDiscountCap(JsonSerializer.Deserialize<DiscountCapJson>(value, Json)!));

    /// <summary>適用条件 ↔ jsonb。</summary>
    public static ValueConverter<OfferConditions, string> OfferConditions { get; } =
        new(
            conditions => JsonSerializer.Serialize(ToJson(conditions), Json),
            value => ToOfferConditions(JsonSerializer.Deserialize<OfferConditionsJson>(value, Json)!));

    /// <summary>適用条件の変更追跡用比較子（不変のためスナップショットは同一参照）。</summary>
    public static ValueComparer<OfferConditions> OfferConditionsComparer { get; } =
        new(
            (left, right) => left!.ApplicableDays.SetEquals(right!.ApplicableDays) && left.TargetSegments.SetEquals(right.TargetSegments),
            conditions => HashCode.Combine(conditions.ApplicableDays.Count, conditions.TargetSegments.Count),
            conditions => conditions);

    private static Money ParseMoney(string value)
    {
        int separator = value.LastIndexOf(' ');
        string amount = value[..separator];
        string currency = value[(separator + 1)..];
        return Domain.Common.Money.Of(decimal.Parse(amount, CultureInfo.InvariantCulture), currency);
    }

    private static TimeRange ToTimeRange(TimeRangeJson json) => Domain.Scheduling.TimeRange.Create(json.Start, json.End);

    private static DiscountCapJson ToJson(DiscountCap cap) => cap.Kind == DiscountKind.Amount
        ? new DiscountCapJson((int)cap.Kind, cap.MaxAmount.Amount, cap.MaxAmount.Currency, 0m)
        : new DiscountCapJson((int)cap.Kind, 0m, null, cap.MaxRate);

    private static DiscountCap ToDiscountCap(DiscountCapJson json) => (DiscountKind)json.Kind == DiscountKind.Amount
        ? Domain.Catalog.DiscountCap.Amount(Domain.Common.Money.Of(json.MaxAmount, json.MaxCurrency!))
        : Domain.Catalog.DiscountCap.Rate(json.MaxRate);

    private static OfferConditionsJson ToJson(OfferConditions conditions) => new(
        conditions.ApplicableDays.Select(day => (int)day).ToArray(),
        conditions.TargetSegments.ToArray());

    private static OfferConditions ToOfferConditions(OfferConditionsJson json) =>
        Domain.Catalog.OfferConditions.Create(json.Days.Select(day => (DayOfWeek)day), json.Segments);

    private sealed record TimeRangeJson(DateTimeOffset Start, DateTimeOffset End);

    private sealed record DiscountCapJson(int Kind, decimal MaxAmount, string? MaxCurrency, decimal MaxRate);

    private sealed record OfferConditionsJson(int[] Days, string[] Segments);
}
