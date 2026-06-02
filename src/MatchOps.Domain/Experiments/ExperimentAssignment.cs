// -----------------------------------------------------------------------------
// <copyright file="ExperimentAssignment.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト実験の割当（experiment_assignments）。実験×顧客で一意。arm で配信対象（treatment）/非配信追跡（control）を表す。
// conversion_events と突合してリフトを算出する（ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Experiments;

/// <summary>実験における 1 顧客の群割当。</summary>
public sealed class ExperimentAssignment
{
    private ExperimentAssignment(
        ExperimentId experimentId,
        CampaignId campaignId,
        CustomerId customerId,
        TenantId tenantId,
        ExperimentArm arm,
        DateTimeOffset assignedAt)
    {
        ExperimentId = experimentId;
        CampaignId = campaignId;
        CustomerId = customerId;
        TenantId = tenantId;
        Arm = arm;
        AssignedAt = assignedAt;
    }

    /// <summary>実験 ID。</summary>
    public ExperimentId ExperimentId { get; }

    /// <summary>対象施策。</summary>
    public CampaignId CampaignId { get; }

    /// <summary>対象顧客。</summary>
    public CustomerId CustomerId { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>割り当てられた群。</summary>
    public ExperimentArm Arm { get; }

    /// <summary>割当日時。</summary>
    public DateTimeOffset AssignedAt { get; }

    /// <summary>割当を生成する。</summary>
    /// <param name="experimentId">実験 ID。</param>
    /// <param name="campaignId">対象施策。</param>
    /// <param name="customerId">対象顧客。</param>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="arm">割り当てる群。</param>
    /// <param name="assignedAt">割当日時。</param>
    /// <returns>生成された <see cref="ExperimentAssignment"/>。</returns>
    public static ExperimentAssignment Create(
        ExperimentId experimentId,
        CampaignId campaignId,
        CustomerId customerId,
        TenantId tenantId,
        ExperimentArm arm,
        DateTimeOffset assignedAt)
        => new(experimentId, campaignId, customerId, tenantId, arm, assignedAt);
}
