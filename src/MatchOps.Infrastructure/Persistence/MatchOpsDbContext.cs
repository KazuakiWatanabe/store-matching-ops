// -----------------------------------------------------------------------------
// <copyright file="MatchOpsDbContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// アプリケーションの EF Core DbContext。
// 全業務エンティティに Global Query Filter でテナントスコープを機械的に強制する（ADR-0006）。
// 強い型 ID は ConfigureConventions で GUID 変換を一括登録する。
// テナント未解決時は既定値で照合し「何も返さない」安全側に倒す。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Tenancy;
using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;
using MatchOps.Domain.Experiments;
using MatchOps.Domain.Matching;
using MatchOps.Domain.Scheduling;
using MatchOps.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>MatchOps の EF Core DbContext。</summary>
public sealed class MatchOpsDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    /// <summary>DbContext を生成する。</summary>
    /// <param name="options">DbContext オプション。</param>
    /// <param name="tenantContext">現在のテナントを解決するコンテキスト。</param>
    public MatchOpsDbContext(DbContextOptions<MatchOpsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>顧客。</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>顧客の行動履歴。</summary>
    public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();

    /// <summary>リソース。</summary>
    public DbSet<Resource> Resources => Set<Resource>();

    /// <summary>空き枠。</summary>
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    /// <summary>Offer（メニュー/コース/クーポン）。</summary>
    public DbSet<Offer> Offers => Set<Offer>();

    /// <summary>施策（MatchingCampaign）。</summary>
    public DbSet<MatchingCampaign> MatchingCampaigns => Set<MatchingCampaign>();

    /// <summary>監査ログ。</summary>
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    /// <summary>Outbox 配信メッセージ。</summary>
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    /// <summary>配信ログ。</summary>
    public DbSet<NotificationLogEntry> NotificationLogs => Set<NotificationLogEntry>();

    /// <summary>実験割当（ホールドアウト）。</summary>
    public DbSet<ExperimentAssignment> ExperimentAssignments => Set<ExperimentAssignment>();

    /// <summary>コンバージョン（成果）イベント。</summary>
    public DbSet<ConversionEventEntity> ConversionEvents => Set<ConversionEventEntity>();

    /// <summary>クエリフィルタで照合する現在のテナント（未解決時は既定値＝該当なし）。</summary>
    internal TenantId CurrentTenant => _tenantContext.CurrentTenantId ?? default;

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<TenantId>().HaveConversion<TenantIdConverter>();
        configurationBuilder.Properties<StoreId>().HaveConversion<StoreIdConverter>();
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
        configurationBuilder.Properties<TimeSlotId>().HaveConversion<TimeSlotIdConverter>();
        configurationBuilder.Properties<OfferId>().HaveConversion<OfferIdConverter>();
        configurationBuilder.Properties<CampaignId>().HaveConversion<CampaignIdConverter>();
        configurationBuilder.Properties<ResourceId>().HaveConversion<ResourceIdConverter>();
        configurationBuilder.Properties<CustomerActivityId>().HaveConversion<CustomerActivityIdConverter>();
        configurationBuilder.Properties<ExperimentId>().HaveConversion<ExperimentIdConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchOpsDbContext).Assembly);

        // テナントスコープの機械的強制（ADR-0006）。
        modelBuilder.Entity<Customer>().HasQueryFilter(customer => customer.TenantId == CurrentTenant);
        modelBuilder.Entity<CustomerActivity>().HasQueryFilter(activity => activity.TenantId == CurrentTenant);
        modelBuilder.Entity<Resource>().HasQueryFilter(resource => resource.TenantId == CurrentTenant);
        modelBuilder.Entity<TimeSlot>().HasQueryFilter(slot => slot.TenantId == CurrentTenant);
        modelBuilder.Entity<Offer>().HasQueryFilter(offer => offer.TenantId == CurrentTenant);
        modelBuilder.Entity<MatchingCampaign>().HasQueryFilter(campaign => campaign.TenantId == CurrentTenant);
        modelBuilder.Entity<AuditLogEntry>().HasQueryFilter(entry => entry.TenantId == CurrentTenant);
        modelBuilder.Entity<OutboxMessageEntity>().HasQueryFilter(message => message.TenantId == CurrentTenant);
        modelBuilder.Entity<NotificationLogEntry>().HasQueryFilter(log => log.TenantId == CurrentTenant);
        modelBuilder.Entity<ExperimentAssignment>().HasQueryFilter(assignment => assignment.TenantId == CurrentTenant);
        modelBuilder.Entity<ConversionEventEntity>().HasQueryFilter(conversion => conversion.TenantId == CurrentTenant);
    }
}
