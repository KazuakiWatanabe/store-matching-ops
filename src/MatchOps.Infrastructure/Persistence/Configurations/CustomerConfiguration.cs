// -----------------------------------------------------------------------------
// <copyright file="CustomerConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Customer Aggregate の EF Core マッピング。連絡先はハッシュ列のみ保持する（平文を持たない）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Customers;
using MatchOps.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="Customer"/> のテーブルマッピング。</summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id).HasColumnName("id");
        builder.Property(customer => customer.TenantId).HasColumnName("tenant_id");
        builder.Property(customer => customer.StoreId).HasColumnName("store_id");
        builder.Property(customer => customer.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(customer => customer.PhoneHash).HasColumnName("phone_hash").HasConversion(ValueObjectConverters.ContactHash);
        builder.Property(customer => customer.EmailHash).HasColumnName("email_hash").HasConversion(ValueObjectConverters.ContactHash);
        builder.Property(customer => customer.VisitCount).HasColumnName("visit_count");
        builder.Property(customer => customer.LastVisitOn).HasColumnName("last_visit_on");
        builder.Property(customer => customer.OptInStatus).HasColumnName("opt_in_status");
        builder.Property(customer => customer.LastNotifiedOn).HasColumnName("last_notified_on");

        builder.Ignore(customer => customer.IsOptedOut);

        builder.HasIndex(customer => customer.TenantId);
    }
}
