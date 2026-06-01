// -----------------------------------------------------------------------------
// <copyright file="AiTone.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI 生成文面のトーン。プロンプトの制約として明示する（ADR-0005）。匿名・集約データに付随する生成方針であり PII ではない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>AI 生成文面のトーン。</summary>
public enum AiTone
{
    /// <summary>親しみやすい・カジュアル。</summary>
    Friendly,

    /// <summary>丁寧・フォーマル。</summary>
    Formal,

    /// <summary>販促・お得感を強調。</summary>
    Promotional,
}
