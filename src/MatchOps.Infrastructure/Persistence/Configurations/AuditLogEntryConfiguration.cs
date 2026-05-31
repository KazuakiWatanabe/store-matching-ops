// -----------------------------------------------------------------------------
// <copyright file="AuditLogEntryConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AuditLogEntry の EF Core マッピング（append-only）。
// </summary>
// -----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="AuditLogEntry"/> のテーブルマッピング。</summary>
public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id).HasColumnName("id");
        builder.Property(entry => entry.TenantId).HasColumnName("tenant_id");
        builder.Property(entry => entry.OccurredAt).HasColumnName("occurred_at");
        builder.Property(entry => entry.Action).HasColumnName("action").IsRequired();
        builder.Property(entry => entry.Detail).HasColumnName("detail").HasColumnType("jsonb");

        builder.HasIndex(entry => entry.TenantId);
    }
}
