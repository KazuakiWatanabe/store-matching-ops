// -----------------------------------------------------------------------------
// <copyright file="RunCampaignCommand.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策の実行（候補抽出＋スコアリング）コマンド。対象テナント・店舗・空き枠群を指定する。
// 多重実行防止の Idempotency-Key は API 層（IdempotencyFilter, Stage 0.9）で扱う。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>施策の候補抽出＋スコアリングを実行するコマンド。</summary>
/// <param name="TenantId">所属テナント。</param>
/// <param name="StoreId">所属店舗。</param>
/// <param name="TargetSlots">対象の空き枠群（1 件以上）。</param>
public sealed record RunCampaignCommand(
    TenantId TenantId,
    StoreId StoreId,
    IReadOnlyList<TimeSlotId> TargetSlots);
