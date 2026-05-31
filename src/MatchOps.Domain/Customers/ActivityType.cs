// -----------------------------------------------------------------------------
// <copyright file="ActivityType.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客の行動履歴 (CustomerActivity) の種別。業種横断の抽象語彙（設計 §4）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Customers;

/// <summary>顧客の行動履歴の種別。</summary>
public enum ActivityType
{
    /// <summary>来店。来店統計（回数・最終来店日）の対象。</summary>
    Visit = 0,

    /// <summary>注文（飲食等）。</summary>
    Order = 1,

    /// <summary>施術（美容等）。</summary>
    Treatment = 2,

    /// <summary>予約。</summary>
    Reservation = 3,

    /// <summary>キャンセル。</summary>
    Cancellation = 4,
}
