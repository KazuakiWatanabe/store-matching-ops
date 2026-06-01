// -----------------------------------------------------------------------------
// <copyright file="AssignHoldoutCommand.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト割当コマンド。施策の対象顧客を control/treatment に決定的分割する（ADR-0007）。
// control 比率は既定 10%（運用で 10〜20% を設定）。配信は treatment のみ。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Experiments;

/// <summary>ホールドアウト割当コマンド。</summary>
/// <param name="ExperimentId">実験 ID。</param>
/// <param name="CampaignId">対象施策。</param>
/// <param name="TenantId">所属テナント。</param>
/// <param name="Customers">割当対象の顧客。</param>
/// <param name="ControlRatio">対照群比率（0〜1。既定 0.1）。</param>
public sealed record AssignHoldoutCommand(
    ExperimentId ExperimentId,
    CampaignId CampaignId,
    TenantId TenantId,
    IReadOnlyList<CustomerId> Customers,
    double ControlRatio = 0.1d);
