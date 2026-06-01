// -----------------------------------------------------------------------------
// <copyright file="SendCampaignCommand.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 承認済み施策の配信コマンド（approved → sent）。配信は Outbox に積むのみ（実送信は Worker）。
// approved を経ていない施策は配信できない（ADR-0004）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>承認済み施策を配信（Outbox 積み込み）するコマンド。</summary>
/// <param name="CampaignId">対象の施策。</param>
public sealed record SendCampaignCommand(CampaignId CampaignId);
