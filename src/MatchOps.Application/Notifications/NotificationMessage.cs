// -----------------------------------------------------------------------------
// <copyright file="NotificationMessage.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 実送信に渡す配信メッセージ（INotificationSender の入力）。連絡先等の PII は含めず、顧客は CustomerId で参照する。
// 実送信時の宛先解決（連絡先）は送信実装側の責務（Phase 0 はスタブのため解決しない）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Notifications;

/// <summary>実送信に渡す配信メッセージ（PII を含まない）。</summary>
/// <param name="TenantId">所属テナント。</param>
/// <param name="CampaignId">配信元の施策。</param>
/// <param name="CustomerId">配信先の顧客（連絡先は送信実装が解決）。</param>
/// <param name="OfferId">提案する Offer。</param>
/// <param name="TimeSlotId">対象の空き枠。</param>
/// <param name="Body">配信文面（差し込み済み。PII を含めない）。</param>
public sealed record NotificationMessage(
    TenantId TenantId,
    CampaignId CampaignId,
    CustomerId CustomerId,
    OfferId OfferId,
    TimeSlotId TimeSlotId,
    string Body);
