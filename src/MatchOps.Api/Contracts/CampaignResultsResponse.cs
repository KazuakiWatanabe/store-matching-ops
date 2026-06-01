// -----------------------------------------------------------------------------
// <copyright file="CampaignResultsResponse.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策結果の参照レスポンス。候補は件数・スコア・対象のみを返し、連絡先・顧客識別子等の PII を含めない（CLAUDE.md §9.3, §11）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Contracts;

/// <summary>施策結果の参照レスポンス（PII を含まない概況）。</summary>
/// <param name="CampaignId">施策の識別子。</param>
/// <param name="Status">施策の状態。</param>
/// <param name="CandidateCount">候補件数。</param>
/// <param name="Candidates">候補のスコア概況。</param>
public sealed record CampaignResultsResponse(
    Guid CampaignId,
    string Status,
    int CandidateCount,
    IReadOnlyList<CandidateScoreResponse> Candidates);
