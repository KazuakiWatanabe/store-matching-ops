// -----------------------------------------------------------------------------
// <copyright file="EfOutboxWriter.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IOutboxWriter の EF Core 実装。Application の OutboxMessage（DTO）を OutboxMessageEntity に写像して積む。
// 実送信はしない（Worker が後で実送信）。確定は IUnitOfWork に委ねる（design.md §10）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;
using MatchOps.Application.Notifications;
using MatchOps.Infrastructure.Persistence;

namespace MatchOps.Infrastructure.Notifications;

/// <summary>Outbox への積み込みを行う EF Core 実装。</summary>
public sealed class EfOutboxWriter : IOutboxWriter
{
    private readonly MatchOpsDbContext _dbContext;
    private readonly IClock _clock;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="dbContext">アプリケーション DbContext。</param>
    /// <param name="clock">時刻源。</param>
    /// <exception cref="ArgumentNullException">いずれかの依存が <c>null</c> の場合。</exception>
    public EfOutboxWriter(MatchOpsDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _dbContext.OutboxMessages.Add(new OutboxMessageEntity
        {
            TenantId = message.TenantId,
            CampaignId = message.CampaignId,
            CustomerId = message.CustomerId,
            TimeSlotId = message.TimeSlotId,
            OfferId = message.OfferId,
            Body = message.Body,
            CreatedAt = _clock.Now,
        });

        return Task.CompletedTask;
    }
}
