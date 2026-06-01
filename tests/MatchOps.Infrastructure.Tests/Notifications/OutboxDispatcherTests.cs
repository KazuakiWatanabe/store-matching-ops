using MatchOps.Application.Common;
using MatchOps.Application.Notifications;
using MatchOps.Domain.Common;
using MatchOps.Infrastructure.Notifications;
using MatchOps.Infrastructure.Persistence;
using MatchOps.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchOps.Infrastructure.Tests.Notifications;

public sealed class OutboxDispatcherTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private sealed class MutableClock : IClock
    {
        public DateTimeOffset Now { get; set; } = new(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);

        public DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
    }

    private sealed class StubSender(bool succeed) : INotificationSender
    {
        public int Calls { get; private set; }

        public Task<NotificationSendResult> SendAsync(
            NotificationMessage message, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(succeed ? NotificationSendResult.Success() : NotificationSendResult.Failure("送信失敗"));
        }
    }

    private sealed class StubEligibility(bool allowed) : INotificationEligibility
    {
        public Task<bool> IsAllowedAsync(
            TenantId tenantId, CustomerId customerId, DateOnly today, CancellationToken cancellationToken = default)
            => Task.FromResult(allowed);
    }

    private async Task<Guid> SeedQueuedMessageAsync(TenantId tenant, DateTimeOffset createdAt)
    {
        var message = new OutboxMessageEntity
        {
            TenantId = tenant,
            CampaignId = CampaignId.New(),
            CustomerId = CustomerId.New(),
            TimeSlotId = TimeSlotId.New(),
            OfferId = OfferId.New(),
            Body = "配信文面です。",
            CreatedAt = createdAt,
        };

        await using MatchOpsDbContext seed = fixture.CreateContext(tenant);
        seed.OutboxMessages.Add(message);
        await seed.SaveChangesAsync();
        return message.Id;
    }

    private OutboxDispatcher CreateDispatcher(
        MatchOpsDbContext context, INotificationSender sender, INotificationEligibility eligibility, IClock clock,
        int maxAttempts = 5, int baseBackoffSeconds = 10)
        => new(
            context,
            sender,
            eligibility,
            clock,
            Options.Create(new OutboxOptions { MaxAttempts = maxAttempts, BatchSize = 100, BaseBackoffSeconds = baseBackoffSeconds }));

    [Fact]
    public async Task Dispatch_QueuedAndEligible_SendsAndRecordsSentLog()
    {
        var tenant = TenantId.New();
        var clock = new MutableClock();
        Guid id = await SeedQueuedMessageAsync(tenant, clock.Now);
        var sender = new StubSender(succeed: true);

        await using (MatchOpsDbContext ctx = fixture.CreateContext(tenant: null))
        {
            OutboxDispatcher dispatcher = CreateDispatcher(ctx, sender, new StubEligibility(allowed: true), clock);
            OutboxDispatchSummary summary = await dispatcher.DispatchPendingAsync(100);
            Assert.Equal(new OutboxDispatchSummary(Sent: 1, Failed: 0, Skipped: 0), summary);
        }

        Assert.Equal(1, sender.Calls);
        await using MatchOpsDbContext read = fixture.CreateContext(tenant: null);
        OutboxMessageEntity stored = await read.OutboxMessages.IgnoreQueryFilters().SingleAsync(m => m.Id == id);
        Assert.Equal("sent", stored.Status);
        NotificationLogEntry log = await read.NotificationLogs.IgnoreQueryFilters().SingleAsync(l => l.OutboxMessageId == id);
        Assert.Equal("sent", log.Status);
    }

    [Fact]
    public async Task Dispatch_IneligibleCustomer_SkipsWithoutSending()
    {
        var tenant = TenantId.New();
        var clock = new MutableClock();
        Guid id = await SeedQueuedMessageAsync(tenant, clock.Now);
        var sender = new StubSender(succeed: true);

        await using (MatchOpsDbContext ctx = fixture.CreateContext(tenant: null))
        {
            OutboxDispatcher dispatcher = CreateDispatcher(ctx, sender, new StubEligibility(allowed: false), clock);
            OutboxDispatchSummary summary = await dispatcher.DispatchPendingAsync(100);
            Assert.Equal(new OutboxDispatchSummary(Sent: 0, Failed: 0, Skipped: 1), summary);
        }

        // オプトアウト等で送信せずスキップ。
        Assert.Equal(0, sender.Calls);
        await using MatchOpsDbContext read = fixture.CreateContext(tenant: null);
        OutboxMessageEntity stored = await read.OutboxMessages.IgnoreQueryFilters().SingleAsync(m => m.Id == id);
        Assert.Equal("skipped", stored.Status);
        NotificationLogEntry log = await read.NotificationLogs.IgnoreQueryFilters().SingleAsync(l => l.OutboxMessageId == id);
        Assert.Equal("skipped", log.Status);
    }

    [Fact]
    public async Task Dispatch_SenderFails_RetriesWithBackoff_ThenMarksFailedAtMaxAttempts()
    {
        var tenant = TenantId.New();
        var clock = new MutableClock();
        Guid id = await SeedQueuedMessageAsync(tenant, clock.Now);
        var sender = new StubSender(succeed: false);

        // 1 回目の失敗 → queued のままバックオフ（次回試行時刻が未来）。
        await using (MatchOpsDbContext ctx = fixture.CreateContext(tenant: null))
        {
            OutboxDispatcher dispatcher = CreateDispatcher(ctx, sender, new StubEligibility(allowed: true), clock, maxAttempts: 2, baseBackoffSeconds: 10);
            await dispatcher.DispatchPendingAsync(100);
        }

        await using (MatchOpsDbContext read = fixture.CreateContext(tenant: null))
        {
            OutboxMessageEntity afterFirst = await read.OutboxMessages.IgnoreQueryFilters().SingleAsync(m => m.Id == id);
            Assert.Equal("queued", afterFirst.Status);
            Assert.Equal(1, afterFirst.AttemptCount);
            Assert.Equal(clock.Now.AddSeconds(10), afterFirst.NextAttemptAt);
        }

        // 次回試行時刻より前は処理対象にならない。
        await using (MatchOpsDbContext ctx = fixture.CreateContext(tenant: null))
        {
            OutboxDispatcher dispatcher = CreateDispatcher(ctx, sender, new StubEligibility(allowed: true), clock, maxAttempts: 2, baseBackoffSeconds: 10);
            OutboxDispatchSummary tooEarly = await dispatcher.DispatchPendingAsync(100);
            Assert.Equal(0, tooEarly.Failed);
        }

        // バックオフ経過後の 2 回目で最大試行回数に到達 → 恒久失敗。
        clock.Now = clock.Now.AddSeconds(60);
        await using (MatchOpsDbContext ctx = fixture.CreateContext(tenant: null))
        {
            OutboxDispatcher dispatcher = CreateDispatcher(ctx, sender, new StubEligibility(allowed: true), clock, maxAttempts: 2, baseBackoffSeconds: 10);
            await dispatcher.DispatchPendingAsync(100);
        }

        await using MatchOpsDbContext final = fixture.CreateContext(tenant: null);
        OutboxMessageEntity afterMax = await final.OutboxMessages.IgnoreQueryFilters().SingleAsync(m => m.Id == id);
        Assert.Equal("failed", afterMax.Status);
        Assert.Equal(2, afterMax.AttemptCount);
        Assert.Null(afterMax.NextAttemptAt);
        int failedLogs = await final.NotificationLogs.IgnoreQueryFilters().CountAsync(l => l.OutboxMessageId == id && l.Status == "failed");
        Assert.Equal(2, failedLogs);
    }
}
