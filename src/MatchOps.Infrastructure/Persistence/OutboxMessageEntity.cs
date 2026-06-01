// -----------------------------------------------------------------------------
// <copyright file="OutboxMessageEntity.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox メッセージの永続化エンティティ（design.md §10）。DB 状態変更と同一トランザクションで積み、
// Worker（OutboxDispatchJob, 後続）が実送信する。連絡先等の PII は保持せず顧客は CustomerId で参照する。
// Application の OutboxMessage（DTO）から EfOutboxWriter が写像する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>Outbox に積まれた配信メッセージの 1 レコード。</summary>
public sealed class OutboxMessageEntity
{
    /// <summary>一意識別子。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>所属テナント。</summary>
    public required TenantId TenantId { get; init; }

    /// <summary>配信元の施策。</summary>
    public required CampaignId CampaignId { get; init; }

    /// <summary>配信先の顧客（連絡先は実送信時に解決）。</summary>
    public required CustomerId CustomerId { get; init; }

    /// <summary>対象の空き枠。</summary>
    public required TimeSlotId TimeSlotId { get; init; }

    /// <summary>提案する Offer。</summary>
    public required OfferId OfferId { get; init; }

    /// <summary>配信文面（差し込み済み。PII を含めない）。</summary>
    public required string Body { get; init; }

    /// <summary>配信状態（queued / sent / failed）。</summary>
    public string Status { get; set; } = "queued";

    /// <summary>積み込み日時。</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
