// -----------------------------------------------------------------------------
// <copyright file="ResourceConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Resource Entity の EF Core マッピング。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="Resource"/> のテーブルマッピング。</summary>
public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources");
        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id).HasColumnName("id");
        builder.Property(resource => resource.TenantId).HasColumnName("tenant_id");
        builder.Property(resource => resource.StoreId).HasColumnName("store_id");
        builder.Property(resource => resource.Kind).HasColumnName("kind");
        builder.Property(resource => resource.Name).HasColumnName("name").IsRequired();

        builder.HasIndex(resource => resource.TenantId);
    }
}
