// -----------------------------------------------------------------------------
// <copyright file="CandidateScoreView.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 候補 1 件のスコア概況（読み取りモデル）。連絡先・顧客識別子等の PII を含まない（CLAUDE.md §9.3, §11）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>候補 1 件のスコア概況（PII を含まない）。</summary>
/// <param name="Score">来店可能性スコア（0〜1）。</param>
/// <param name="OfferId">提案する Offer。</param>
/// <param name="TimeSlotId">対象の空き枠。</param>
public sealed record CandidateScoreView(double Score, OfferId OfferId, TimeSlotId TimeSlotId);
