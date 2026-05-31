// -----------------------------------------------------------------------------
// <copyright file="MatchScored.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策のスコアリングが完了したことを表すドメインイベント。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>施策のスコアリング完了イベント。</summary>
/// <param name="CampaignId">対象施策。</param>
/// <param name="CandidateCount">スコアリングされた候補数。</param>
public sealed record MatchScored(CampaignId CampaignId, int CandidateCount) : IDomainEvent;
