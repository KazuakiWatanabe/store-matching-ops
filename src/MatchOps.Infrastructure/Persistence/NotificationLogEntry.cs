// -----------------------------------------------------------------------------
// <copyright file="NotificationLogEntry.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 配信ログの永続化エンティティ（design.md §10）。配信の成功/失敗/スキップを記録する。
// 連絡先等の PII を平文で残さない（CLAUDE.md §9.2）。顧客は CustomerId で参照する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>配信ログの 1 レコード。</summary>
public sealed class NotificationLogEntry
{
    /// <summary>一意識別子。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>所属テナント。</summary>
    public required TenantId TenantId { get; init; }

    /// <summary>対象の Outbox メッセージ。</summary>
    public required Guid OutboxMessageId { get; init; }

    /// <summary>配信元の施策。</summary>
    public required CampaignId CampaignId { get; init; }

    /// <summary>配信先の顧客（PII なし）。</summary>
    public required CustomerId CustomerId { get; init; }

    /// <summary>結果（sent / failed / skipped）。</summary>
    public required string Status { get; init; }

    /// <summary>付帯情報（失敗理由・スキップ理由等。PII・シークレットを含めない）。</summary>
    public string? Detail { get; init; }

    /// <summary>記録日時。</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
