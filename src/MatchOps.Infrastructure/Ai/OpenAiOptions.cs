// -----------------------------------------------------------------------------
// <copyright file="OpenAiOptions.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// OpenAI / Azure OpenAI 連携の設定。エンドポイント・モデルは設定から、API キーは Secrets/環境変数から注入する。
// API キーを appsettings.json に書かない（CLAUDE.md §9.1）。ログにも出さない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Infrastructure.Ai;

/// <summary>OpenAI / Azure OpenAI 連携の設定。</summary>
public sealed class OpenAiOptions
{
    /// <summary>設定セクション名。</summary>
    public const string SectionName = "OpenAi";

    /// <summary>API のベース URL（例: "https://api.openai.com"）。</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Chat Completions のパス。</summary>
    public string ChatCompletionsPath { get; set; } = "/v1/chat/completions";

    /// <summary>使用するモデル名（例: "gpt-4o-mini"）。</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>API キー（Secrets/環境変数から注入。appsettings に書かない）。</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>要求タイムアウト（秒）。</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
