// -----------------------------------------------------------------------------
// <copyright file="CampaignResultsView.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策の結果参照ビュー（管理画面向け読み取りモデル）。
// 連絡先（メール・電話）等の PII は含めず、候補は件数・スコア・対象（Offer/枠）のみを返す（CLAUDE.md §9.3, §11）。
// 顧客識別子も含めない（本ビューは概況把握用。個別ドリルダウンは別途）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>施策の結果参照ビュー（PII を含まない概況）。</summary>
/// <param name="CampaignId">施策 ID。</param>
/// <param name="Status">施策の状態（文字列表現）。</param>
/// <param name="CandidateCount">候補件数。</param>
/// <param name="Candidates">候補のスコア概況（連絡先・顧客識別子を含まない）。</param>
public sealed record CampaignResultsView(
    CampaignId CampaignId,
    string Status,
    int CandidateCount,
    IReadOnlyList<CandidateScoreView> Candidates);
