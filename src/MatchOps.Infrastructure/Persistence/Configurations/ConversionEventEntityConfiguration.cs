// -----------------------------------------------------------------------------
// <copyright file="ConversionEventEntityConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ConversionEventEntity の EF Core マッピング（conversion_events）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="ConversionEventEntity"/> のテーブルマッピング。</summary>
public sealed class ConversionEventEntityConfiguration : IEntityTypeConfiguration<ConversionEventEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConversionEventEntity> builder)
    {
        builder.ToTable("conversion_events");
        builder.HasKey(conversion => conversion.Id);

        builder.Property(conversion => conversion.Id).HasColumnName("id");
        builder.Property(conversion => conversion.TenantId).HasColumnName("tenant_id");
        builder.Property(conversion => conversion.CampaignId).HasColumnName("campaign_id");
        builder.Property(conversion => conversion.CustomerId).HasColumnName("customer_id");
        builder.Property(conversion => conversion.Kind).HasColumnName("kind").IsRequired();
        builder.Property(conversion => conversion.Revenue).HasColumnName("revenue");
        builder.Property(conversion => conversion.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(conversion => conversion.TenantId);
        builder.HasIndex(conversion => conversion.CampaignId);
    }
}
