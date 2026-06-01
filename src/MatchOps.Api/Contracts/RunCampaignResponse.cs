// -----------------------------------------------------------------------------
// <copyright file="RunCampaignResponse.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策実行レスポンス。作成された施策の識別子を返す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Contracts;

/// <summary>施策実行レスポンス。</summary>
/// <param name="CampaignId">作成された施策の識別子。</param>
public sealed record RunCampaignResponse(Guid CampaignId);
