// -----------------------------------------------------------------------------
// <copyright file="EfExperimentAssignmentRepository.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IExperimentAssignmentRepository の EF Core 実装。テナントスコープは Global Query Filter が強制する（ADR-0006）。
// 確定は IUnitOfWork に委ねる。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Experiments;
using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Experiments;

/// <summary>実験割当の EF Core リポジトリ。</summary>
public sealed class EfExperimentAssignmentRepository : IExperimentAssignmentRepository
{
    private readonly MatchOpsDbContext _dbContext;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> が <c>null</c> の場合。</exception>
    public EfExperimentAssignmentRepository(MatchOpsDbContext dbContext)
        => _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public Task AddAsync(ExperimentAssignment assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        _dbContext.ExperimentAssignments.Add(assignment);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExperimentAssignment>> GetByExperimentAsync(
        ExperimentId experimentId, CancellationToken cancellationToken = default)
        => await _dbContext.ExperimentAssignments
            .Where(assignment => assignment.ExperimentId == experimentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
