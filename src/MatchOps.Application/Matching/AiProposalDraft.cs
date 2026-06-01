// -----------------------------------------------------------------------------
// <copyright file="AiProposalDraft.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI 提案生成の出力。施策／セグメント単位の提案理由テンプレートと配信文面テンプレートを保持する。
// 個別顧客への文面は「テンプレート＋差し込み」で生成し、LLM は顧客ごとに呼ばない（CLAUDE.md §4.3）。
// 承認前の人手編集を許容する（ADR-0004）ため、テンプレートは編集可能な文字列として扱う。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Matching;

/// <summary>AI が生成した施策単位の提案ドラフト。</summary>
/// <param name="ReasonTemplate">提案理由のテンプレート（説明可能性の文章化）。</param>
/// <param name="MessageTemplate">配信文面のテンプレート（差し込み前）。</param>
public sealed record AiProposalDraft(string ReasonTemplate, string MessageTemplate);
