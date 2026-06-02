// -----------------------------------------------------------------------------
// <copyright file="EfConversionReadStore.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IConversionReadStore の EF Core 実装。施策に紐づくコンバージョンを読み取り、リフト算出に供給する（ADR-0007）。
// テナントスコープは Global Query Filter が強制する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Experiments;
using MatchOps.Domain.Common;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Experiments;

/// <summary>コンバージョン読み取りの EF Core 実装。</summary>
public sealed class EfConversionReadStore : IConversionReadStore
{
    private readonly MatchOpsDbContext _dbContext;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> が <c>null</c> の場合。</exception>
    public EfConversionReadStore(MatchOpsDbContext dbContext)
        => _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversionRecord>> GetByCampaignAsync(
        CampaignId campaignId, CancellationToken cancellationToken = default)
        => await _dbContext.ConversionEvents
            .Where(conversion => conversion.CampaignId == campaignId)
            .Select(conversion => new ConversionRecord(conversion.CustomerId, conversion.Revenue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
