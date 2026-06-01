// -----------------------------------------------------------------------------
// <copyright file="EfUnitOfWork.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IUnitOfWork の EF Core 実装。DbContext の SaveChanges で、Aggregate の更新と Outbox 積み込みを
// 同一トランザクションとして確定する（design.md §10）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>EF Core による Unit of Work 実装。</summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly MatchOpsDbContext _dbContext;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> が <c>null</c> の場合。</exception>
    public EfUnitOfWork(MatchOpsDbContext dbContext)
        => _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
