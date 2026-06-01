// -----------------------------------------------------------------------------
// <copyright file="AiCampaignContext.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策要約の入力（匿名化・集約済み, ADR-0005）。件数・セグメント名・統計・許容 Offer のみを持ち、
// 個別顧客の識別子・氏名・連絡先・購買明細を型として持たない（LLM への PII 送出を構造的に防ぐ）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>施策要約の入力（集約・匿名化済み）。</summary>
/// <param name="StoreCategory">店舗の業種テンプレート種別（例: "beauty" / "restaurant"）。</param>
/// <param name="SlotSummary">空き枠の要約（日時帯・対応メニュー種別等の集約文。PII なし）。</param>
/// <param name="CandidateCount">候補件数（集約値）。</param>
/// <param name="SegmentNames">対象セグメント名の配列（例: "休眠顧客", "平日利用者"）。</param>
/// <param name="TopReasons">主要なスコア寄与理由の配列（例: "45日以上未来店"）。</param>
/// <param name="AllowedOffers">提示してよい Offer の制約。</param>
/// <param name="Tone">生成文面のトーン。</param>
public sealed record AiCampaignContext(
    string StoreCategory,
    string SlotSummary,
    int CandidateCount,
    IReadOnlyList<string> SegmentNames,
    IReadOnlyList<string> TopReasons,
    IReadOnlyList<AllowedOffer> AllowedOffers,
    AiTone Tone);
