// -----------------------------------------------------------------------------
// <copyright file="TimeSlotConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// TimeSlot Aggregate の EF Core マッピング。時間範囲・対応 Offer 種別は jsonb 列で保持する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Scheduling;
using MatchOps.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="TimeSlot"/> のテーブルマッピング。</summary>
public sealed class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("time_slots");
        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.Id).HasColumnName("id");
        builder.Property(slot => slot.TenantId).HasColumnName("tenant_id");
        builder.Property(slot => slot.StoreId).HasColumnName("store_id");
        builder.Property(slot => slot.ResourceId).HasColumnName("resource_id");
        builder.Property(slot => slot.Range)
            .HasColumnName("time_range")
            .HasColumnType("jsonb")
            .HasConversion(ValueObjectConverters.TimeRange);
        builder.Property(slot => slot.SupportedOfferCategories)
            .HasColumnName("supported_offer_categories")
            .HasColumnType("jsonb")
            .HasConversion(ValueObjectConverters.OfferCategories, ValueObjectConverters.OfferCategoriesComparer);
        builder.Property(slot => slot.Status).HasColumnName("status");

        builder.HasIndex(slot => slot.TenantId);
        builder.HasIndex(slot => slot.ResourceId);
    }
}
