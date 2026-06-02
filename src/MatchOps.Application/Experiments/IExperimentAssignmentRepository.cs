// -----------------------------------------------------------------------------
// <copyright file="IExperimentAssignmentRepository.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 実験割当 (ExperimentAssignment) の永続化抽象。テナントスコープは Infrastructure の Global Query Filter で強制する（ADR-0006）。
// 確定は IUnitOfWork に委ねる。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Application.Experiments;

/// <summary>実験割当の永続化を担う抽象。</summary>
public interface IExperimentAssignmentRepository
{
    /// <summary>割当を追加する（確定は <see cref="Common.IUnitOfWork"/>）。</summary>
    /// <param name="assignment">追加する割当。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>追加処理を表すタスク。</returns>
    Task AddAsync(ExperimentAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>指定実験の割当を取得する。</summary>
    /// <param name="experimentId">実験 ID。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>割当の一覧。</returns>
    Task<IReadOnlyList<ExperimentAssignment>> GetByExperimentAsync(
        ExperimentId experimentId, CancellationToken cancellationToken = default);
}
