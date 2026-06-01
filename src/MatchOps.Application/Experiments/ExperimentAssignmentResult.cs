// -----------------------------------------------------------------------------
// <copyright file="ExperimentAssignmentResult.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト割当の結果。配信対象（treatment）顧客の集合と群別件数を持つ。
// 配信は TreatmentCustomers のみを対象とする（control は非配信で追跡, ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Experiments;

/// <summary>ホールドアウト割当の結果。</summary>
/// <param name="TreatmentCount">処置群の人数。</param>
/// <param name="ControlCount">対照群の人数。</param>
/// <param name="TreatmentCustomers">配信対象（処置群）の顧客集合。control は含まない。</param>
public sealed record ExperimentAssignmentResult(
    int TreatmentCount,
    int ControlCount,
    IReadOnlyList<CustomerId> TreatmentCustomers);
