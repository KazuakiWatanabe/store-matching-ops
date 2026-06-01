// -----------------------------------------------------------------------------
// <copyright file="ConversionEventEntity.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// コンバージョン（成果）イベントの永続化エンティティ（conversion_events, design.md §8/§10）。
// 来店・予約・クーポン利用等の成果を記録し、実験割当 (arm) と突合してリフトを算出する（ADR-0007）。
// 記録（取込）は Phase 1。本 Stage はスキーマと読み取り（リフト集計）を用意する。PII は保持しない（CustomerId 参照）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>コンバージョン（成果）イベントの 1 レコード。</summary>
public sealed class ConversionEventEntity
{
    /// <summary>一意識別子。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>所属テナント。</summary>
    public required TenantId TenantId { get; init; }

    /// <summary>関連する施策。</summary>
    public required CampaignId CampaignId { get; init; }

    /// <summary>CV した顧客。</summary>
    public required CustomerId CustomerId { get; init; }

    /// <summary>成果種別（例: visit / reservation / coupon）。</summary>
    public required string Kind { get; init; }

    /// <summary>成果の売上。</summary>
    public decimal Revenue { get; init; }

    /// <summary>発生日時。</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
