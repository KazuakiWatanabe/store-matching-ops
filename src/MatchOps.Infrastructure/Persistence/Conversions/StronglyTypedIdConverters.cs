// -----------------------------------------------------------------------------
// <copyright file="StronglyTypedIdConverters.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 強い型 ID（readonly record struct(Guid)）と GUID の相互変換 ValueConverter 群。
// DbContext の ConfigureConventions で型単位に登録し、全プロパティへ一括適用する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;
using MatchOps.Domain.Scheduling;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MatchOps.Infrastructure.Persistence.Conversions;

/// <summary><see cref="TenantId"/> と GUID の変換。</summary>
public sealed class TenantIdConverter() : ValueConverter<TenantId, Guid>(id => id.Value, value => new TenantId(value));

/// <summary><see cref="StoreId"/> と GUID の変換。</summary>
public sealed class StoreIdConverter() : ValueConverter<StoreId, Guid>(id => id.Value, value => new StoreId(value));

/// <summary><see cref="CustomerId"/> と GUID の変換。</summary>
public sealed class CustomerIdConverter() : ValueConverter<CustomerId, Guid>(id => id.Value, value => new CustomerId(value));

/// <summary><see cref="TimeSlotId"/> と GUID の変換。</summary>
public sealed class TimeSlotIdConverter() : ValueConverter<TimeSlotId, Guid>(id => id.Value, value => new TimeSlotId(value));

/// <summary><see cref="OfferId"/> と GUID の変換。</summary>
public sealed class OfferIdConverter() : ValueConverter<OfferId, Guid>(id => id.Value, value => new OfferId(value));

/// <summary><see cref="CampaignId"/> と GUID の変換。</summary>
public sealed class CampaignIdConverter() : ValueConverter<CampaignId, Guid>(id => id.Value, value => new CampaignId(value));

/// <summary><see cref="ResourceId"/> と GUID の変換。</summary>
public sealed class ResourceIdConverter() : ValueConverter<ResourceId, Guid>(id => id.Value, value => new ResourceId(value));

/// <summary><see cref="CustomerActivityId"/> と GUID の変換。</summary>
public sealed class CustomerActivityIdConverter() : ValueConverter<CustomerActivityId, Guid>(id => id.Value, value => new CustomerActivityId(value));

/// <summary><see cref="ExperimentId"/> と GUID の変換。</summary>
public sealed class ExperimentIdConverter() : ValueConverter<ExperimentId, Guid>(id => id.Value, value => new ExperimentId(value));
