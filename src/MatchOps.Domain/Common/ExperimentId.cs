// -----------------------------------------------------------------------------
// <copyright file="ExperimentId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 効果測定の実験 (Experiment) の一意識別子（強い型）。施策（またはセグメント）に対するホールドアウト実験を表す（ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>ホールドアウト実験の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct ExperimentId(Guid Value)
{
    /// <summary>新しい実験 ID を生成する。</summary>
    /// <returns>一意な <see cref="ExperimentId"/>。</returns>
    public static ExperimentId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
