// -----------------------------------------------------------------------------
// <copyright file="AiMessageContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 配信文面（テンプレート）生成の入力（匿名化・集約済み, ADR-0005）。
// 文面はテンプレート＋差し込み用であり、宛名・連絡先等の個別 PII は含めない。生成は施策／セグメント単位で 1 回。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>配信文面テンプレート生成の入力（集約・匿名化済み）。</summary>
/// <param name="StoreCategory">店舗の業種テンプレート種別。</param>
/// <param name="SegmentName">対象セグメント名（例: "休眠顧客"）。</param>
/// <param name="SlotSummary">空き枠の要約（集約文）。</param>
/// <param name="Offer">提示する Offer の制約（種別・値引き上限）。</param>
/// <param name="Tone">生成文面のトーン。</param>
public sealed record AiMessageContext(
    string StoreCategory,
    string SegmentName,
    string SlotSummary,
    AllowedOffer Offer,
    AiTone Tone);
