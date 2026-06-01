// -----------------------------------------------------------------------------
// <copyright file="ProposeCampaignCommand.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI 提案生成を依頼し施策を proposed に進めるコマンド（scored → proposed）。
// 提案は施策／セグメント単位で 1 回生成し、LLM には匿名化・集約データのみ渡す（CLAUDE.md §4.3）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>AI 提案を生成し施策を proposed に進めるコマンド。</summary>
/// <param name="CampaignId">対象の施策。</param>
public sealed record ProposeCampaignCommand(CampaignId CampaignId);
