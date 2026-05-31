// -----------------------------------------------------------------------------
// <copyright file="CustomerActivityConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// CustomerActivity Entity の EF Core マッピング（append-only）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Customers;
using MatchOps.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="CustomerActivity"/> のテーブルマッピング。</summary>
public sealed class CustomerActivityConfiguration : IEntityTypeConfiguration<CustomerActivity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CustomerActivity> builder)
    {
        builder.ToTable("customer_activities");
        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id).HasColumnName("id");
        builder.Property(activity => activity.TenantId).HasColumnName("tenant_id");
        builder.Property(activity => activity.CustomerId).HasColumnName("customer_id");
        builder.Property(activity => activity.Type).HasColumnName("type");
        builder.Property(activity => activity.OccurredOn).HasColumnName("occurred_on");
        builder.Property(activity => activity.Amount).HasColumnName("amount").HasConversion(ValueObjectConverters.Money);

        builder.HasIndex(activity => activity.TenantId);
        builder.HasIndex(activity => activity.CustomerId);
    }
}
