// -----------------------------------------------------------------------------
// <copyright file="IExperimentQueries.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 実験の参照（リフト集計）ユースケース抽象。割当 (arm) と conversion_events を突合してリフトを算出する（ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Application.Experiments;

/// <summary>実験のリフト集計を提供する抽象。</summary>
public interface IExperimentQueries
{
    /// <summary>指定実験のリフトを算出する。割当が無い場合は <c>null</c>。</summary>
    /// <param name="experimentId">実験 ID。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>リフト測定結果。割当が無ければ <c>null</c>。</returns>
    Task<LiftResult?> GetLiftAsync(ExperimentId experimentId, CancellationToken cancellationToken = default);
}
