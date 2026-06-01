// -----------------------------------------------------------------------------
// <copyright file="IClock.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 時刻源の抽象。本番コードで DateTime.UtcNow を直呼びせず、本インターフェース経由で取得する（CLAUDE.md §10.4）。
// Domain には時刻をパラメータ（DateTimeOffset now / DateOnly today）で渡し、Domain は IClock を知らない。
// テストでは固定時刻の実装に差し替える。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Common;

/// <summary>現在時刻を提供する抽象。</summary>
public interface IClock
{
    /// <summary>現在の日時（タイムゾーン付き）。</summary>
    DateTimeOffset Now { get; }

    /// <summary>現在の日付（頻度判定等に使用）。</summary>
    DateOnly Today { get; }
}
