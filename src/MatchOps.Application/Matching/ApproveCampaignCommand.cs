// -----------------------------------------------------------------------------
// <copyright file="ApproveCampaignCommand.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 人手による施策承認コマンド（proposed → approved, ADR-0004 Human-in-the-loop）。
// 承認者は監査ログ・施策に記録する。承認なしに配信へ進めることはできない。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>人手で施策を承認するコマンド。</summary>
/// <param name="CampaignId">対象の施策。</param>
/// <param name="ApprovedBy">承認者（操作者の識別子。監査記録に用いる）。</param>
public sealed record ApproveCampaignCommand(CampaignId CampaignId, string ApprovedBy);
