// -----------------------------------------------------------------------------
// <copyright file="LiftResult.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// リフト測定結果（ADR-0007 / design.md §10）。
// リフト = 処置群CVR − 対照群CVR。増分件数 = リフト × 処置群人数。増分売上 = 増分件数 × 客単価（処置群の CV あたり売上）。
// 「施策をしなくても来た顧客」を対照群で差し引いた純粋な効果（増分）を表す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Experiments;

/// <summary>処置群と対照群から算出したリフト測定結果。</summary>
/// <param name="TreatmentCount">処置群の人数。</param>
/// <param name="ControlCount">対照群の人数。</param>
/// <param name="TreatmentConversionRate">処置群 CVR。</param>
/// <param name="ControlConversionRate">対照群 CVR。</param>
/// <param name="Lift">リフト（処置群CVR − 対照群CVR）。</param>
/// <param name="IncrementalConversions">増分 CV 件数（リフト × 処置群人数）。</param>
/// <param name="IncrementalRevenue">増分売上（増分 CV 件数 × 処置群の客単価）。</param>
public sealed record LiftResult(
    int TreatmentCount,
    int ControlCount,
    double TreatmentConversionRate,
    double ControlConversionRate,
    double Lift,
    double IncrementalConversions,
    decimal IncrementalRevenue)
{
    /// <summary>
    /// 処置群・対照群の成果からリフトを算出する。
    /// </summary>
    /// <param name="treatment">処置群の成果。</param>
    /// <param name="control">対照群の成果。</param>
    /// <returns>算出された <see cref="LiftResult"/>。</returns>
    /// <exception cref="ArgumentNullException">引数が <c>null</c> の場合。</exception>
    public static LiftResult Calculate(ArmOutcome treatment, ArmOutcome control)
    {
        ArgumentNullException.ThrowIfNull(treatment);
        ArgumentNullException.ThrowIfNull(control);

        double lift = treatment.ConversionRate - control.ConversionRate;
        double incrementalConversions = lift * treatment.Count;

        // 客単価（処置群の CV あたり売上）。CV が無ければ 0。
        decimal averageOrderValue = treatment.Conversions == 0
            ? 0m
            : treatment.Revenue / treatment.Conversions;
        decimal incrementalRevenue = (decimal)incrementalConversions * averageOrderValue;

        return new LiftResult(
            treatment.Count,
            control.Count,
            treatment.ConversionRate,
            control.ConversionRate,
            lift,
            incrementalConversions,
            incrementalRevenue);
    }
}
