// -----------------------------------------------------------------------------
// <copyright file="ExperimentArm.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト実験の群（arm）。treatment は配信、control は非配信で追跡する（ADR-0007）。
// リフト = 処置群CVR − 対照群CVR の算出に用いる。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Experiments;

/// <summary>ホールドアウト実験の群。</summary>
public enum ExperimentArm
{
    /// <summary>対照群（非配信・追跡のみ）。</summary>
    Control = 0,

    /// <summary>処置群（配信対象）。</summary>
    Treatment = 1,
}
