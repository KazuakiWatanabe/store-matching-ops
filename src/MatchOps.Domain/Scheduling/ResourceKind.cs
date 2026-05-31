// -----------------------------------------------------------------------------
// <copyright file="ResourceKind.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// リソース種別。業種で意味が変わる抽象（設計 §4）。
// 飲食: 席・個室／美容: スタッフ・施術台 など。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Scheduling;

/// <summary>リソースの種別。</summary>
public enum ResourceKind
{
    /// <summary>席・テーブル（飲食等）。</summary>
    Seat = 0,

    /// <summary>個室。</summary>
    Room = 1,

    /// <summary>スタッフ（美容師・施術者等）。</summary>
    Staff = 2,

    /// <summary>設備・施術台等。</summary>
    Equipment = 3,
}
