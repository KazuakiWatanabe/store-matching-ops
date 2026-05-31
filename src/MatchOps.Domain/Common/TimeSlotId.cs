// -----------------------------------------------------------------------------
// <copyright file="TimeSlotId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 空き枠 (TimeSlot) の一意識別子（強い型）。席・スタッフ・時間帯で表される供給単位を表す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>空き枠 (TimeSlot) の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct TimeSlotId(Guid Value)
{
    /// <summary>新しい空き枠 ID を生成する。</summary>
    /// <returns>一意な <see cref="TimeSlotId"/>。</returns>
    public static TimeSlotId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
