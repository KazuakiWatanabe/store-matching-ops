// -----------------------------------------------------------------------------
// <copyright file="NotificationLogEntryConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// NotificationLogEntry の EF Core マッピング。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="NotificationLogEntry"/> のテーブルマッピング。</summary>
public sealed class NotificationLogEntryConfiguration : IEntityTypeConfiguration<NotificationLogEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotificationLogEntry> builder)
    {
        builder.ToTable("notification_logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id).HasColumnName("id");
        builder.Property(log => log.TenantId).HasColumnName("tenant_id");
        builder.Property(log => log.OutboxMessageId).HasColumnName("outbox_message_id");
        builder.Property(log => log.CampaignId).HasColumnName("campaign_id");
        builder.Property(log => log.CustomerId).HasColumnName("customer_id");
        builder.Property(log => log.Status).HasColumnName("status").IsRequired();
        builder.Property(log => log.Detail).HasColumnName("detail");
        builder.Property(log => log.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(log => log.TenantId);
        builder.HasIndex(log => log.CampaignId);
    }
}
