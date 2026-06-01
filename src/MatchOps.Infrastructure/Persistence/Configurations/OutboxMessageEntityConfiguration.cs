// -----------------------------------------------------------------------------
// <copyright file="OutboxMessageEntityConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// OutboxMessageEntity の EF Core マッピング。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="OutboxMessageEntity"/> のテーブルマッピング。</summary>
public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id).HasColumnName("id");
        builder.Property(message => message.TenantId).HasColumnName("tenant_id");
        builder.Property(message => message.CampaignId).HasColumnName("campaign_id");
        builder.Property(message => message.CustomerId).HasColumnName("customer_id");
        builder.Property(message => message.TimeSlotId).HasColumnName("time_slot_id");
        builder.Property(message => message.OfferId).HasColumnName("offer_id");
        builder.Property(message => message.Body).HasColumnName("body").IsRequired();
        builder.Property(message => message.Status).HasColumnName("status").IsRequired();
        builder.Property(message => message.CreatedAt).HasColumnName("created_at");
        builder.Property(message => message.AttemptCount).HasColumnName("attempt_count");
        builder.Property(message => message.NextAttemptAt).HasColumnName("next_attempt_at");

        builder.HasIndex(message => message.TenantId);
        builder.HasIndex(message => message.Status);
    }
}
