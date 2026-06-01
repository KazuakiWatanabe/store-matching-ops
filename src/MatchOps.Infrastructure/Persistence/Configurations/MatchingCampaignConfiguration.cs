// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaignConfiguration.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// MatchingCampaign Aggregate の EF Core マッピング（Stage 0.7 で繰り延べた施策永続化）。
// 状態・承認メタはスカラ列、対象枠と候補（スコア内訳・提案理由）は jsonb 列に保持する。
// ドメインイベントは永続化しない（揮発）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Matching;
using MatchOps.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchOps.Infrastructure.Persistence.Configurations;

/// <summary><see cref="MatchingCampaign"/> のテーブルマッピング。</summary>
public sealed class MatchingCampaignConfiguration : IEntityTypeConfiguration<MatchingCampaign>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MatchingCampaign> builder)
    {
        builder.ToTable("matching_campaigns");
        builder.HasKey(campaign => campaign.Id);

        builder.Property(campaign => campaign.Id).HasColumnName("id");
        builder.Property(campaign => campaign.TenantId).HasColumnName("tenant_id");
        builder.Property(campaign => campaign.StoreId).HasColumnName("store_id");
        builder.Property(campaign => campaign.Status)
            .HasColumnName("status")
            .HasConversion<string>();
        builder.Property(campaign => campaign.ApprovedBy).HasColumnName("approved_by");
        builder.Property(campaign => campaign.ApprovedAt).HasColumnName("approved_at");
        builder.Property(campaign => campaign.SentAt).HasColumnName("sent_at");

        builder.Property(campaign => campaign.TargetSlots)
            .HasColumnName("target_slots")
            .HasColumnType("jsonb")
            .HasConversion(MatchingCampaignConverters.TargetSlots, MatchingCampaignConverters.TargetSlotsComparer);

        builder.Property(campaign => campaign.Candidates)
            .HasColumnName("candidates")
            .HasColumnType("jsonb")
            .HasConversion(MatchingCampaignConverters.Candidates, MatchingCampaignConverters.CandidatesComparer);

        // ドメインイベントは永続化しない。
        builder.Ignore(campaign => campaign.DomainEvents);

        builder.HasIndex(campaign => campaign.TenantId);
    }
}
