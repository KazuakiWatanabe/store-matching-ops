// -----------------------------------------------------------------------------
// <copyright file="Resource.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 割り当て対象リソース (席・個室・スタッフ・施術台) を表す Entity。
// テナント・店舗に属する。業種差は ResourceKind と名称で表現する（設計 §4）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Scheduling;

/// <summary>割り当て対象のリソースを表す Entity。</summary>
public sealed class Resource
{
    private Resource(ResourceId id, TenantId tenantId, StoreId storeId, ResourceKind kind, string name)
    {
        Id = id;
        TenantId = tenantId;
        StoreId = storeId;
        Kind = kind;
        Name = name;
    }

    /// <summary>リソースの一意識別子。</summary>
    public ResourceId Id { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>所属店舗。</summary>
    public StoreId StoreId { get; }

    /// <summary>リソース種別。</summary>
    public ResourceKind Kind { get; }

    /// <summary>表示名（例: カウンター席、スタイリストA）。</summary>
    public string Name { get; }

    /// <summary>
    /// リソースを新規作成（生成）する。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="kind">リソース種別。</param>
    /// <param name="name">表示名（必須）。</param>
    /// <returns>生成された <see cref="Resource"/>。</returns>
    /// <exception cref="DomainException">表示名が空白の場合。</exception>
    public static Resource Create(TenantId tenantId, StoreId storeId, ResourceKind kind, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("リソース名は必須です。");
        }

        return new Resource(ResourceId.New(), tenantId, storeId, kind, name.Trim());
    }
}
