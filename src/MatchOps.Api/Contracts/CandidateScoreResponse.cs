// -----------------------------------------------------------------------------
// <copyright file="CandidateScoreResponse.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 候補 1 件のスコア概況レスポンス。連絡先・顧客識別子等の PII を含めない（CLAUDE.md §9.3, §11）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Contracts;

/// <summary>候補 1 件のスコア概況レスポンス（PII を含まない）。</summary>
/// <param name="Score">来店可能性スコア（0〜1）。</param>
/// <param name="OfferId">提案する Offer の識別子。</param>
/// <param name="TimeSlotId">対象の空き枠識別子。</param>
public sealed record CandidateScoreResponse(double Score, Guid OfferId, Guid TimeSlotId);
