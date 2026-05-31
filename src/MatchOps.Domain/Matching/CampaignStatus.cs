// -----------------------------------------------------------------------------
// <copyright file="CampaignStatus.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策 (MatchingCampaign) の状態。
// draft → scored → proposed → approved → sent → measured。
// proposed → approved は人手のみ（ADR-0004）。approved を経ずに sent へ遷移できない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Matching;

/// <summary>施策の状態。</summary>
public enum CampaignStatus
{
    /// <summary>下書き（対象枠を設定済み・未スコア）。</summary>
    Draft = 0,

    /// <summary>スコアリング済み（候補とスコアを保持）。</summary>
    Scored = 1,

    /// <summary>提案済み（承認待ち）。</summary>
    Proposed = 2,

    /// <summary>承認済み（人手による承認、ADR-0004）。</summary>
    Approved = 3,

    /// <summary>配信済み。</summary>
    Sent = 4,

    /// <summary>効果測定済み。</summary>
    Measured = 5,
}
