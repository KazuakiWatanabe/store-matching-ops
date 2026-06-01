// -----------------------------------------------------------------------------
// <copyright file="AiCampaignSummary.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策要約の出力。要約テキストと注意フラグを持つ。AI 障害時はフォールバック（要約なし）として空テキスト＋注意フラグを返し、施策フローを止めない（運用継続性）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>施策要約の出力。</summary>
/// <param name="Summary">要約テキスト（AI 障害時は空文字）。</param>
/// <param name="Cautions">注意フラグ（上限超過検出・フォールバック使用等の日本語メッセージ）。</param>
public sealed record AiCampaignSummary(string Summary, IReadOnlyList<string> Cautions);
