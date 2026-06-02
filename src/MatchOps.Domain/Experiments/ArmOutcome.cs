// -----------------------------------------------------------------------------
// <copyright file="ArmOutcome.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 1 つの群（arm）の集計成果。対象人数・コンバージョン（来店等）件数・売上を保持し、リフト算出の入力とする（ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Experiments;

/// <summary>群の集計成果（人数・CV 件数・売上）。</summary>
public sealed record ArmOutcome
{
    private ArmOutcome(int count, int conversions, decimal revenue)
    {
        Count = count;
        Conversions = conversions;
        Revenue = revenue;
    }

    /// <summary>対象人数。</summary>
    public int Count { get; }

    /// <summary>コンバージョン（CV）件数。</summary>
    public int Conversions { get; }

    /// <summary>CV の売上合計。</summary>
    public decimal Revenue { get; }

    /// <summary>CV 率（CV 件数 ÷ 人数。人数 0 のときは 0）。</summary>
    public double ConversionRate => Count == 0 ? 0d : (double)Conversions / Count;

    /// <summary>群成果を検証して生成する。</summary>
    /// <param name="count">対象人数（0 以上）。</param>
    /// <param name="conversions">CV 件数（0 以上・人数以下）。</param>
    /// <param name="revenue">売上合計（0 以上）。</param>
    /// <returns>生成された <see cref="ArmOutcome"/>。</returns>
    /// <exception cref="DomainException">人数・CV・売上が不正な場合。</exception>
    public static ArmOutcome Of(int count, int conversions, decimal revenue)
    {
        if (count < 0)
        {
            throw new DomainException("対象人数は 0 以上で指定してください。");
        }

        if (conversions < 0 || conversions > count)
        {
            throw new DomainException("CV 件数は 0 以上かつ対象人数以下で指定してください。");
        }

        if (revenue < 0m)
        {
            throw new DomainException("売上は 0 以上で指定してください。");
        }

        return new ArmOutcome(count, conversions, revenue);
    }
}
