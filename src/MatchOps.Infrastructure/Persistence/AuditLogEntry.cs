// -----------------------------------------------------------------------------
// <copyright file="AuditLogEntry.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 監査ログの永続化エンティティ（append-only）。AI 提案・承認・編集・配信・削除要求等を記録する（CLAUDE.md §10.2）。
// アプリロールからは UPDATE/DELETE を REVOKE する（infra/db/audit_logs_revoke.sql）。
// Domain の Aggregate ではなく、永続化・運用都合の Infrastructure エンティティとして扱う。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>監査ログの 1 レコード（append-only）。</summary>
public sealed class AuditLogEntry
{
    /// <summary>一意識別子。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>所属テナント。</summary>
    public required TenantId TenantId { get; init; }

    /// <summary>発生日時。</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>操作種別（例: campaign.approved）。</summary>
    public required string Action { get; init; }

    /// <summary>付帯情報（jsonb。任意）。</summary>
    public string? Detail { get; init; }
}
