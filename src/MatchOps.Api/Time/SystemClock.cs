// -----------------------------------------------------------------------------
// <copyright file="SystemClock.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// システム時計に基づく IClock 実装。本番の時刻源。テストは固定時刻の実装に差し替える（CLAUDE.md §10.4）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;

namespace MatchOps.Api.Time;

/// <summary>システム時計に基づく時刻源。</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset Now => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
}
