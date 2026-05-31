// -----------------------------------------------------------------------------
// <copyright file="OptInStatus.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 顧客の配信オプトイン状態。配信制御（オプトアウト尊重）の基礎（ADR-0005, 設計 §9）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Customers;

/// <summary>顧客の配信オプトイン状態。</summary>
public enum OptInStatus
{
    /// <summary>意思表示が未確認（既定）。配信には明示的なオプトインを要する。</summary>
    Unknown = 0,

    /// <summary>配信に同意済み（オプトイン）。</summary>
    OptedIn = 1,

    /// <summary>配信を拒否済み（オプトアウト）。配信対象から除外する。</summary>
    OptedOut = 2,
}
