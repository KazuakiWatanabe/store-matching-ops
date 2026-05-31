// -----------------------------------------------------------------------------
// <copyright file="SlotStatus.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 空き枠 (TimeSlot) の状態。状態遷移は ADR-0009 を参照。
// open → held（仮押さえ）→ booked（確定）/ open（解放）。closed は手動/期限切れ。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Scheduling;

/// <summary>空き枠の状態。</summary>
public enum SlotStatus
{
    /// <summary>公開中（提案・予約可能）。</summary>
    Open = 0,

    /// <summary>仮押さえ中（提案配信〜確定待ち）。</summary>
    Held = 1,

    /// <summary>予約確定。</summary>
    Booked = 2,

    /// <summary>クローズ（手動または期限切れ。以降は遷移不可）。</summary>
    Closed = 3,
}
