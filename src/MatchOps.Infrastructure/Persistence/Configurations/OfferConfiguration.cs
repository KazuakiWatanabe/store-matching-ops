// -----------------------------------------------------------------------------
// <copyright file="OfferConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Offer Aggregate の EF Core マッピング。値引き上限・適用条件は jsonb 列で保持する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Catalog;
using MatchOps.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="Offer"/> のテーブルマッピング。</summary>
public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");
        builder.HasKey(offer => offer.Id);

        builder.Property(offer => offer.Id).HasColumnName("id");
        builder.Property(offer => offer.TenantId).HasColumnName("tenant_id");
        builder.Property(offer => offer.StoreId).HasColumnName("store_id");
        builder.Property(offer => offer.Type).HasColumnName("type");
        builder.Property(offer => offer.Name).HasColumnName("name").IsRequired();
        builder.Property(offer => offer.DiscountCap)
            .HasColumnName("discount_cap")
            .HasColumnType("jsonb")
            .HasConversion(ValueObjectConverters.DiscountCap);
        builder.Property(offer => offer.Conditions)
            .HasColumnName("conditions")
            .HasColumnType("jsonb")
            .HasConversion(ValueObjectConverters.OfferConditions, ValueObjectConverters.OfferConditionsComparer);
        builder.Property(offer => offer.IsActive).HasColumnName("is_active");

        builder.HasIndex(offer => offer.TenantId);
    }
}
