// -----------------------------------------------------------------------------
// <copyright file="ExperimentAssignmentConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ExperimentAssignment の EF Core マッピング（experiment_assignments）。実験×顧客で一意（複合主キー）。arm は文字列で保持。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Experiments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="ExperimentAssignment"/> のテーブルマッピング。</summary>
public sealed class ExperimentAssignmentConfiguration : IEntityTypeConfiguration<ExperimentAssignment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExperimentAssignment> builder)
    {
        builder.ToTable("experiment_assignments");
        builder.HasKey(assignment => new { assignment.ExperimentId, assignment.CustomerId });

        builder.Property(assignment => assignment.ExperimentId).HasColumnName("experiment_id");
        builder.Property(assignment => assignment.CampaignId).HasColumnName("campaign_id");
        builder.Property(assignment => assignment.CustomerId).HasColumnName("customer_id");
        builder.Property(assignment => assignment.TenantId).HasColumnName("tenant_id");
        builder.Property(assignment => assignment.Arm).HasColumnName("arm").HasConversion<string>();
        builder.Property(assignment => assignment.AssignedAt).HasColumnName("assigned_at");

        builder.HasIndex(assignment => assignment.TenantId);
        builder.HasIndex(assignment => assignment.CampaignId);
    }
}
