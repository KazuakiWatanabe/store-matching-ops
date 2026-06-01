// -----------------------------------------------------------------------------
// <copyright file="ApproveCampaignRequest.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策承認リクエスト（Human-in-the-loop, ADR-0004）。
// 承認者は省略可。省略時は認証コンテキストの操作者（X-User-Id）を承認者とする。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Contracts;

/// <summary>施策承認リクエスト。</summary>
/// <param name="ApprovedBy">承認者（省略時は認証コンテキストの操作者を用いる）。</param>
public sealed record ApproveCampaignRequest(string? ApprovedBy = null);
