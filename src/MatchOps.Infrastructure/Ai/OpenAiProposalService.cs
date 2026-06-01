// -----------------------------------------------------------------------------
// <copyright file="OpenAiProposalService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IAiProposalService の OpenAI / Azure OpenAI 実装（ADR-0005）。プロンプト構築をここに隔離する。
// - プロンプトには匿名化・集約済みデータのみを載せる（識別子・氏名・連絡先・購買明細を一切含めない）。
// - 値引き上限・トーンをプロンプトの制約として明示し、出力の値引き率が上限を超える場合は安全側へ置換する（ADR-0010）。
// - LLM 障害時は例外を投げず、フォールバック出力（注意フラグ付き）を返し施策フローを止めない（運用継続性）。
// - API キー・プロンプト本文をログに出さない（CLAUDE.md §9.2）。
// </summary>
// -----------------------------------------------------------------------------

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using MatchOps.Application.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchOps.Infrastructure.Ai;

/// <summary>OpenAI 互換 Chat Completions API を用いた AI 提案サービス実装。</summary>
public sealed partial class OpenAiProposalService : IAiProposalService
{
    private static readonly string[] FallbackCaution = ["AI 生成に失敗したため既定文面を使用しました。"];

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiProposalService> _logger;

    /// <summary>依存を注入してサービスを構築する。</summary>
    /// <param name="httpClient">LLM 呼び出し用 HttpClient。</param>
    /// <param name="options">OpenAI 連携設定。</param>
    /// <param name="logger">ロガー（PII・プロンプト本文・キーは出力しない）。</param>
    /// <exception cref="ArgumentNullException">いずれかの依存が <c>null</c> の場合。</exception>
    public OpenAiProposalService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiProposalService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AiCampaignSummary> SummarizeCampaignAsync(
        AiCampaignContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string offerConstraints = string.Join(
            " / ", context.AllowedOffers.Select(DescribeOffer));
        string userPrompt =
            $"業種: {context.StoreCategory}\n" +
            $"空き枠: {context.SlotSummary}\n" +
            $"候補件数: {context.CandidateCount}\n" +
            $"セグメント: {string.Join(", ", context.SegmentNames)}\n" +
            $"主要理由: {string.Join(", ", context.TopReasons)}\n" +
            $"許容オファー: {offerConstraints}\n" +
            $"トーン: {DescribeTone(context.Tone)}\n" +
            "上記の集約情報のみを用いて、店舗管理者向けに施策の要約を日本語で簡潔に作成してください。個人を特定する表現は使わないでください。";

        string? content = await TryCompleteAsync(
            "あなたは店舗マーケティングの提案を要約するアシスタントです。", userPrompt, cancellationToken)
            .ConfigureAwait(false);

        return content is null
            ? new AiCampaignSummary(string.Empty, FallbackCaution)
            : new AiCampaignSummary(content.Trim(), []);
    }

    /// <inheritdoc />
    public async Task<AiMessageDraft> GenerateMessageAsync(
        AiMessageContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        int capPercent = context.Offer.MaxDiscountPercent ?? 0;
        string userPrompt =
            $"業種: {context.StoreCategory}\n" +
            $"対象セグメント: {context.SegmentName}\n" +
            $"空き枠: {context.SlotSummary}\n" +
            $"許容オファー: {DescribeOffer(context.Offer)}\n" +
            $"トーン: {DescribeTone(context.Tone)}\n" +
            $"制約: 値引き率は最大 {capPercent}% を超えないこと。宛名や連絡先などの個人情報は含めないこと（差し込みは別途行う）。\n" +
            "上記に従い、配信文面のテンプレートを日本語で作成してください。";

        string? content = await TryCompleteAsync(
            "あなたは店舗の配信文面を作成するアシスタントです。", userPrompt, cancellationToken)
            .ConfigureAwait(false);

        if (content is null)
        {
            return new AiMessageDraft(DefaultMessageTemplate(context), FallbackCaution);
        }

        content = content.Trim();

        // 値引き上限の逸脱検証（ADR-0010）。上限超過を検出したら安全側の既定文面へ置換する。
        if (ExceedsDiscountCap(content, capPercent))
        {
            _logger.LogWarning(
                "AI 生成文面が値引き上限 {Cap}% を超えたため既定文面に置換しました。", capPercent);
            return new AiMessageDraft(
                DefaultMessageTemplate(context),
                [$"値引き上限（{capPercent}%）を超える表現を検出したため既定文面に置換しました。"]);
        }

        return new AiMessageDraft(content, []);
    }

    /// <inheritdoc />
    public async Task<AiResultComment> CommentResultsAsync(
        AiResultContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string userPrompt =
            $"業種: {context.StoreCategory}\n" +
            $"対象セグメント: {context.SegmentName}\n" +
            $"配信数: {context.SentCount}\n" +
            $"来店数: {context.VisitedCount}\n" +
            $"トーン: {DescribeTone(context.Tone)}\n" +
            "上記の集約統計のみを用いて、施策結果の所見と次回への示唆を日本語で簡潔に述べてください。個人を特定する表現は使わないでください。";

        string? content = await TryCompleteAsync(
            "あなたは店舗施策の結果を分析するアシスタントです。", userPrompt, cancellationToken)
            .ConfigureAwait(false);

        return content is null
            ? new AiResultComment(string.Empty, FallbackCaution)
            : new AiResultComment(content.Trim(), []);
    }

    private static string DescribeOffer(AllowedOffer offer)
        => offer.MaxDiscountPercent is { } cap
            ? $"{offer.OfferKind}（最大 {cap}% 引き）"
            : $"{offer.OfferKind}（値引きなし）";

    private static string DescribeTone(AiTone tone)
        => tone switch
        {
            AiTone.Formal => "丁寧・フォーマル",
            AiTone.Promotional => "販促・お得感",
            _ => "親しみやすい",
        };

    private static string DefaultMessageTemplate(AiMessageContext context)
        => $"いつもご利用ありがとうございます。{context.SlotSummary}に空きが出ました。ぜひご来店ください。";

    private static bool ExceedsDiscountCap(string content, int capPercent)
    {
        foreach (Match match in PercentPattern().Matches(content))
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                && value > capPercent)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string?> TryCompleteAsync(
        string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt },
                },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ChatCompletionsPath)
            {
                Content = JsonContent.Create(payload),
            };
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI 呼び出しが失敗しました（ステータス {Status}）。", (int)response.StatusCode);
                return null;
            }

            await using Stream stream = await response.Content
                .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument document = await JsonDocument
                .ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            string? content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            _logger.LogWarning(ex, "AI 呼び出し中に例外が発生したためフォールバックします。");
            return null;
        }
    }

    [GeneratedRegex(@"(\d+)\s*[%％]")]
    private static partial Regex PercentPattern();
}
