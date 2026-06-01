// -----------------------------------------------------------------------------
// <copyright file="OutboxMessage.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Outbox に積む配信メッセージ（design.md §10, Outbox パターン）。
// DB 状態変更と同一トランザクションで積み、実送信は Worker（OutboxDispatchJob, Stage 0.11）が行う。
// 連絡先（メール・電話）等の PII は保持せず、顧客は CustomerId で参照する。実送信時に Worker が解決する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Notifications;

/// <summary>Outbox に積まれる単一の配信メッセージ。</summary>
/// <param name="CampaignId">配信元の施策。</param>
/// <param name="TenantId">所属テナント（テナントスコープ強制のため必須）。</param>
/// <param name="CustomerId">配信先の顧客（連絡先は実送信時に Worker が解決）。</param>
/// <param name="TimeSlotId">対象の空き枠。</param>
/// <param name="OfferId">提案する Offer。</param>
/// <param name="Body">配信文面（テンプレート差し込み済み。PII を含めない）。</param>
public sealed record OutboxMessage(
    CampaignId CampaignId,
    TenantId TenantId,
    CustomerId CustomerId,
    TimeSlotId TimeSlotId,
    OfferId OfferId,
    string Body);
