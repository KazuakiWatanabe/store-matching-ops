// -----------------------------------------------------------------------------
// <copyright file="SlotCandidacy.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 候補抽出のための空き枠の最小情報（Matching ローカルの入力抽象）。
// 他モジュール（Scheduling）の Domain 型に依存しないため、Application が TimeSlot から写像する（CLAUDE.md §4.1）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>候補抽出に用いる空き枠の最小情報。</summary>
/// <param name="TimeSlotId">空き枠 ID。</param>
/// <param name="TenantId">所属テナント。</param>
/// <param name="StoreId">所属店舗。</param>
public sealed record SlotCandidacy(TimeSlotId TimeSlotId, TenantId TenantId, StoreId StoreId);
