// -----------------------------------------------------------------------------
// <copyright file="AiResultComment.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策結果コメントの出力。コメントテキストと注意フラグを持つ。AI 障害時は空テキスト＋注意フラグを返す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>施策結果コメントの出力。</summary>
/// <param name="Comment">結果コメントテキスト（AI 障害時は空文字）。</param>
/// <param name="Cautions">注意フラグ（フォールバック使用等の日本語メッセージ）。</param>
public sealed record AiResultComment(string Comment, IReadOnlyList<string> Cautions);
