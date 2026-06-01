using System.Net;
using System.Net.Http.Json;
using MatchOps.Api.Contracts;
using MatchOps.Api.Idempotency;
using MatchOps.Api.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace MatchOps.Api.Tests;

public class MatchingCampaignsApiTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    private static HttpRequestMessage RunRequest(Guid tenant, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/campaigns/run")
        {
            Content = JsonContent.Create(new RunCampaignRequest(Guid.NewGuid(), [Guid.NewGuid()])),
        };
        request.Headers.Add(TenantResolutionMiddleware.TenantHeader, tenant.ToString());
        if (idempotencyKey is not null)
        {
            request.Headers.Add(IdempotencyFilter.HeaderName, idempotencyKey);
        }

        return request;
    }

    [Fact]
    public async Task Run_WithoutIdempotencyKey_Returns400()
    {
        using var factory = new CampaignApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.SendAsync(RunRequest(TenantA, idempotencyKey: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("idempotency_key_required", error!.ErrorCode);
    }

    [Fact]
    public async Task Run_WithoutTenant_Returns401()
    {
        using var factory = new CampaignApiFactory();
        using HttpClient client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/campaigns/run")
        {
            Content = JsonContent.Create(new RunCampaignRequest(Guid.NewGuid(), [Guid.NewGuid()])),
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, "key-1");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Run_DuplicateIdempotencyKey_CreatesCampaignOnce()
    {
        using var factory = new CampaignApiFactory();
        using HttpClient client = factory.CreateClient();

        // 同一キー・同一本文で 2 回送信するため、本文を固定する。
        var body = new RunCampaignRequest(Guid.NewGuid(), [Guid.NewGuid()]);

        HttpResponseMessage first = await SendRun(client, body, "dup-key");
        HttpResponseMessage second = await SendRun(client, body, "dup-key");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // 再生レスポンスは初回と同一本文（表記 = camelCase も一致）。
        string firstRaw = await first.Content.ReadAsStringAsync();
        string secondRaw = await second.Content.ReadAsStringAsync();
        Assert.Equal(firstRaw, secondRaw);
        Assert.Contains("campaignId", secondRaw);

        RunCampaignResponse firstBody = (await first.Content.ReadFromJsonAsync<RunCampaignResponse>())!;
        Assert.NotEqual(Guid.Empty, firstBody.CampaignId);

        // 副作用は 1 回（施策は 1 件のみ作成される）。
        InMemoryCampaignStore store = factory.Services.GetRequiredService<InMemoryCampaignStore>();
        Assert.Single(store.Campaigns);
    }

    [Fact]
    public async Task Send_WithoutApproval_Returns409()
    {
        using var factory = new CampaignApiFactory();
        using HttpClient client = factory.CreateClient();

        Guid campaignId = await RunAndGetId(client, TenantA);

        // 承認していない（scored のまま）施策の配信 → 拒否（ADR-0004）。
        var send = new HttpRequestMessage(HttpMethod.Post, $"/api/campaigns/{campaignId}/send");
        send.Headers.Add(TenantResolutionMiddleware.TenantHeader, TenantA.ToString());
        send.Headers.Add(IdempotencyFilter.HeaderName, "send-key");

        HttpResponseMessage response = await client.SendAsync(send);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("not_approved", error!.ErrorCode);
    }

    [Fact]
    public async Task GetResults_OtherTenant_Returns404()
    {
        using var factory = new CampaignApiFactory();
        using HttpClient client = factory.CreateClient();

        Guid campaignId = await RunAndGetId(client, TenantA);

        // 別テナント（B）からの取得 → 404。
        var otherTenantGet = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{campaignId}/results");
        otherTenantGet.Headers.Add(TenantResolutionMiddleware.TenantHeader, TenantB.ToString());
        HttpResponseMessage otherResponse = await client.SendAsync(otherTenantGet);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);

        // 同一テナント（A）からの取得 → 200（対照）。
        var ownerGet = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{campaignId}/results");
        ownerGet.Headers.Add(TenantResolutionMiddleware.TenantHeader, TenantA.ToString());
        HttpResponseMessage ownerResponse = await client.SendAsync(ownerGet);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    [Fact]
    public async Task FullFlow_RunProposeApproveSend_Succeeds()
    {
        using var factory = new CampaignApiFactory();
        using HttpClient client = factory.CreateClient();

        Guid campaignId = await RunAndGetId(client, TenantA);

        Assert.Equal(HttpStatusCode.NoContent, (await Post(client, $"/api/campaigns/{campaignId}/propose")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await Post(client, $"/api/campaigns/{campaignId}/approve", user: "admin@example.com")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(client, $"/api/campaigns/{campaignId}/send")).StatusCode);

        var resultsGet = new HttpRequestMessage(HttpMethod.Get, $"/api/campaigns/{campaignId}/results");
        resultsGet.Headers.Add(TenantResolutionMiddleware.TenantHeader, TenantA.ToString());
        HttpResponseMessage results = await client.SendAsync(resultsGet);
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
        CampaignResultsResponse body = (await results.Content.ReadFromJsonAsync<CampaignResultsResponse>())!;
        Assert.Equal("Sent", body.Status);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string url, string? user = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(TenantResolutionMiddleware.TenantHeader, TenantA.ToString());
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        if (user is not null)
        {
            request.Headers.Add(TenantResolutionMiddleware.UserHeader, user);
        }

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendRun(HttpClient client, RunCampaignRequest body, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/campaigns/run")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(TenantResolutionMiddleware.TenantHeader, TenantA.ToString());
        request.Headers.Add(IdempotencyFilter.HeaderName, key);
        return client.SendAsync(request);
    }

    private static async Task<Guid> RunAndGetId(HttpClient client, Guid tenant)
    {
        HttpResponseMessage response = await client.SendAsync(RunRequest(tenant, "run-" + Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RunCampaignResponse body = (await response.Content.ReadFromJsonAsync<RunCampaignResponse>())!;
        return body.CampaignId;
    }
}
