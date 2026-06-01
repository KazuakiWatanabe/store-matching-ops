// -----------------------------------------------------------------------------
// <copyright file="AiResultContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策結果コメント生成の入力（匿名化・集約済み, ADR-0005）。配信数・反応数・来店数等の統計のみを持ち、個別顧客を含まない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>施策結果コメント生成の入力（集約・匿名化済み）。</summary>
/// <param name="StoreCategory">店舗の業種テンプレート種別。</param>
/// <param name="SegmentName">対象セグメント名。</param>
/// <param name="SentCount">配信数（集約値）。</param>
/// <param name="VisitedCount">来店数（集約値）。</param>
/// <param name="Tone">生成文面のトーン。</param>
public sealed record AiResultContext(
    string StoreCategory,
    string SegmentName,
    int SentCount,
    int VisitedCount,
    AiTone Tone);
