// -----------------------------------------------------------------------------
// <copyright file="AiProposalRequest.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI 提案生成の入力（ADR-0005 / CLAUDE.md §4.3, §9.4）。
// 施策単位の「匿名化・集約済み」データのみを保持する。値はすべて施策の候補集合から集約して算出する。
// 個別顧客の識別子・氏名・連絡先・購買明細は型として持たせない（LLM への PII 送出を構造的に防ぐ）。
// 生成は施策単位で 1 回。顧客ごとに LLM を呼ばない。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>AI 提案生成の入力（集約・匿名化済み）。</summary>
/// <param name="StoreId">店舗識別子（業種テンプレート選択に使用。個人 PII ではない）。</param>
/// <param name="CandidateCount">候補件数（集約値）。</param>
/// <param name="SlotCount">対象の空き枠数（集約値）。</param>
/// <param name="OfferVariety">提案 Offer の種類数（集約値・識別子は含めない）。</param>
/// <param name="AverageScore">候補スコアの平均（0〜1）。</param>
/// <param name="SegmentSummary">集約値から構築した日本語サマリ（例: "対象顧客 12 名 / 空き枠 3 件"）。PII を含めない。</param>
public sealed record AiProposalRequest(
    StoreId StoreId,
    int CandidateCount,
    int SlotCount,
    int OfferVariety,
    double AverageScore,
    string SegmentSummary);
