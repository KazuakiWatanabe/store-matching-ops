// -----------------------------------------------------------------------------
// <copyright file="CustomerCandidacy.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 候補抽出のための顧客の最小情報（Matching ローカルの入力抽象）。
// 配信可否は Customer.CanReceiveNotifications() の結果を Application が写像する（CLAUDE.md §4.1）。
// 頻度判定のための最終通知日を保持し、頻度ルール自体は Matching が適用する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>候補抽出に用いる顧客の最小情報。</summary>
/// <param name="CustomerId">顧客 ID。</param>
/// <param name="TenantId">所属テナント。</param>
/// <param name="StoreId">所属店舗。</param>
/// <param name="CanReceiveNotifications">配信可能か（オプトイン済みか）。</param>
/// <param name="LastNotifiedOn">最終通知日（未通知なら <c>null</c>）。</param>
public sealed record CustomerCandidacy(
    CustomerId CustomerId,
    TenantId TenantId,
    StoreId StoreId,
    bool CanReceiveNotifications,
    DateOnly? LastNotifiedOn);
