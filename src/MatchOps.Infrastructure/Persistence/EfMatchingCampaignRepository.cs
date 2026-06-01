// -----------------------------------------------------------------------------
// <copyright file="EfMatchingCampaignRepository.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IMatchingCampaignRepository の EF Core 実装。テナントスコープは DbContext の Global Query Filter が強制するため、
// 取得は現在テナント外の施策に到達しない（ADR-0006）。確定は IUnitOfWork に委ねる。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Matching;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>施策の EF Core リポジトリ。</summary>
public sealed class EfMatchingCampaignRepository : IMatchingCampaignRepository
{
    private readonly MatchOpsDbContext _dbContext;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> が <c>null</c> の場合。</exception>
    public EfMatchingCampaignRepository(MatchOpsDbContext dbContext)
        => _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public Task AddAsync(MatchingCampaign campaign, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        _dbContext.MatchingCampaigns.Add(campaign);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<MatchingCampaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
        => await _dbContext.MatchingCampaigns
            .FirstOrDefaultAsync(campaign => campaign.Id == id, cancellationToken)
            .ConfigureAwait(false);
}
