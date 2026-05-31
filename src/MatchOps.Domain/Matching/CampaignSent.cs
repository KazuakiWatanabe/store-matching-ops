// -----------------------------------------------------------------------------
// <copyright file="CampaignSent.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策が配信されたことを表すドメインイベント。実送信は Application/Outbox が担う。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>施策の配信イベント。</summary>
/// <param name="CampaignId">対象施策。</param>
/// <param name="SentAt">配信日時。</param>
public sealed record CampaignSent(CampaignId CampaignId, DateTimeOffset SentAt) : IDomainEvent;
