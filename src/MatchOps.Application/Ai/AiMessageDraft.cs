// -----------------------------------------------------------------------------
// <copyright file="AiMessageDraft.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 配信文面テンプレートの出力。テンプレート本文と注意フラグを持つ。
// 値引き上限超過が検出された場合は安全側の文面に置換し、注意フラグを立てる（ADR-0010）。AI 障害時は既定テンプレートを返す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>配信文面テンプレートの出力。</summary>
/// <param name="MessageTemplate">配信文面テンプレート（差し込み前）。</param>
/// <param name="Cautions">注意フラグ（上限超過置換・フォールバック使用等の日本語メッセージ）。</param>
public sealed record AiMessageDraft(string MessageTemplate, IReadOnlyList<string> Cautions);
