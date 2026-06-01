using System.Text.Json;
using MatchOps.Application.Ai;
using MatchOps.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MatchOps.Infrastructure.Tests.Ai;

public sealed class OpenAiProposalServiceTests : IDisposable
{
    private const string ChatPath = "/v1/chat/completions";

    private readonly WireMockServer _server = WireMockServer.Start();

    private OpenAiProposalService CreateService()
    {
        var options = new OpenAiOptions
        {
            Endpoint = _server.Url!,
            ChatCompletionsPath = ChatPath,
            Model = "test-model",
            ApiKey = string.Empty,
        };
        var client = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        return new OpenAiProposalService(client, Options.Create(options), NullLogger<OpenAiProposalService>.Instance);
    }

    private void StubChatContent(string content, int statusCode = 200)
        => _server
            .Given(Request.Create().WithPath(ChatPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ChatResponse(content)));

    private void StubFailure(int statusCode)
        => _server
            .Given(Request.Create().WithPath(ChatPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(statusCode));

    private static string ChatResponse(string content)
        => $"{{\"choices\":[{{\"message\":{{\"content\":{JsonSerializer.Serialize(content)}}}}}]}}";

    /// <summary>送信されたリクエスト JSON を解析し、全メッセージ本文（system+user プロンプト）を復号して連結する。</summary>
    private string CapturedPrompt()
    {
        string raw = _server.LogEntries.Single().RequestMessage!.Body ?? string.Empty;
        using JsonDocument document = JsonDocument.Parse(raw);
        return string.Concat(document.RootElement
            .GetProperty("messages")
            .EnumerateArray()
            .Select(message => message.GetProperty("content").GetString()));
    }

    [Fact]
    public async Task GenerateMessageAsync_Success_ReturnsContent_AndPromptContainsNoPii()
    {
        StubChatContent("休眠中のお客様へお得なご案内です。");
        OpenAiProposalService service = CreateService();
        var context = new AiMessageContext(
            "beauty", "休眠顧客", "平日午後の空き枠", new AllowedOffer("クーポン", 20), AiTone.Friendly);

        AiMessageDraft draft = await service.GenerateMessageAsync(context);

        Assert.Equal("休眠中のお客様へお得なご案内です。", draft.MessageTemplate);
        Assert.Empty(draft.Cautions);

        // 送信されたプロンプトに集約情報は含まれ、個人を特定する情報は含まれない（ADR-0005）。
        string prompt = CapturedPrompt();
        Assert.Contains("休眠顧客", prompt);
        foreach (string pii in new[] { "山田太郎", "taro@example.com", "090-1234-5678", "東京都" })
        {
            Assert.DoesNotContain(pii, prompt);
        }
    }

    [Fact]
    public async Task GenerateMessageAsync_ExceedsDiscountCap_ReplacedWithCaution()
    {
        StubChatContent("本日限定、今だけ 50% OFF！ぜひご来店ください。");
        OpenAiProposalService service = CreateService();
        var context = new AiMessageContext(
            "restaurant", "平日利用者", "平日ランチの空き枠", new AllowedOffer("クーポン", 20), AiTone.Promotional);

        AiMessageDraft draft = await service.GenerateMessageAsync(context);

        // 上限 20% を超える 50% が含まれるため、既定文面に置換され注意フラグが立つ。
        Assert.DoesNotContain("50", draft.MessageTemplate);
        Assert.NotEmpty(draft.Cautions);
    }

    [Fact]
    public async Task GenerateMessageAsync_WithinDiscountCap_Kept()
    {
        StubChatContent("本日は 15% OFF クーポンをご用意しました。");
        OpenAiProposalService service = CreateService();
        var context = new AiMessageContext(
            "restaurant", "平日利用者", "平日ランチの空き枠", new AllowedOffer("クーポン", 20), AiTone.Promotional);

        AiMessageDraft draft = await service.GenerateMessageAsync(context);

        Assert.Contains("15", draft.MessageTemplate);
        Assert.Empty(draft.Cautions);
    }

    [Fact]
    public async Task GenerateMessageAsync_LlmFailure_ReturnsDefaultTemplateWithCaution()
    {
        StubFailure(500);
        OpenAiProposalService service = CreateService();
        var context = new AiMessageContext(
            "beauty", "新規顧客", "週末の空き枠", new AllowedOffer("メニュー", null), AiTone.Friendly);

        AiMessageDraft draft = await service.GenerateMessageAsync(context);

        // LLM 障害でも例外を投げず、既定文面＋注意フラグで施策フローを継続できる。
        Assert.False(string.IsNullOrWhiteSpace(draft.MessageTemplate));
        Assert.NotEmpty(draft.Cautions);
    }

    [Fact]
    public async Task SummarizeCampaignAsync_LlmFailure_ReturnsEmptySummaryWithCaution()
    {
        StubFailure(503);
        OpenAiProposalService service = CreateService();
        var context = new AiCampaignContext(
            "beauty", "週末の空き枠", 12, ["休眠顧客"], ["45日以上未来店"],
            [new AllowedOffer("クーポン", 20)], AiTone.Friendly);

        AiCampaignSummary summary = await service.SummarizeCampaignAsync(context);

        Assert.Equal(string.Empty, summary.Summary);
        Assert.NotEmpty(summary.Cautions);
    }

    [Fact]
    public async Task SummarizeCampaignAsync_Success_PromptContainsAggregatesNotPii()
    {
        StubChatContent("休眠顧客 12 名への施策です。");
        OpenAiProposalService service = CreateService();
        var context = new AiCampaignContext(
            "beauty", "週末の空き枠", 12, ["休眠顧客"], ["45日以上未来店"],
            [new AllowedOffer("クーポン", 20)], AiTone.Friendly);

        AiCampaignSummary summary = await service.SummarizeCampaignAsync(context);

        Assert.Equal("休眠顧客 12 名への施策です。", summary.Summary);
        string prompt = CapturedPrompt();
        Assert.Contains("休眠顧客", prompt);
        Assert.Contains("45日以上未来店", prompt);
        Assert.DoesNotContain("@", prompt);
    }

    public void Dispose() => _server.Dispose();
}
