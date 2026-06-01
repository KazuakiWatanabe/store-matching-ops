// -----------------------------------------------------------------------------
// <copyright file="RunCampaignRequest.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策実行（候補抽出＋スコアリング）リクエスト。テナントは認証コンテキストから解決し、本文には含めない（ADR-0006）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Contracts;

/// <summary>施策実行リクエスト。</summary>
/// <param name="StoreId">対象店舗の識別子。</param>
/// <param name="TargetSlotIds">対象の空き枠識別子（1 件以上）。</param>
public sealed record RunCampaignRequest(Guid StoreId, IReadOnlyList<Guid> TargetSlotIds);
