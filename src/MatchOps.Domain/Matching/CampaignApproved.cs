// -----------------------------------------------------------------------------
// <copyright file="CampaignApproved.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策が人手で承認されたことを表すドメインイベント（ADR-0004）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>施策の承認イベント（人手承認）。</summary>
/// <param name="CampaignId">対象施策。</param>
/// <param name="ApprovedBy">承認者。</param>
/// <param name="ApprovedAt">承認日時。</param>
public sealed record CampaignApproved(CampaignId CampaignId, string ApprovedBy, DateTimeOffset ApprovedAt) : IDomainEvent;
