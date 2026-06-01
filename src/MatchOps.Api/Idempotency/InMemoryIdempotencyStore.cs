// -----------------------------------------------------------------------------
// <copyright file="InMemoryIdempotencyStore.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// インメモリの冪等ストア（Phase 0）。24 時間の TTL で保存済みレスポンスを保持する（CLAUDE.md §10.1）。
// プロセス内のみ有効。Phase 1 で Redis 等の分散ストアに置き換える。
// </summary>
// -----------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace MatchOps.Api.Idempotency;

/// <summary>プロセス内メモリに保持する冪等ストア。</summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IdempotencyRecord? TryGet(string scopedKey, DateTimeOffset now)
    {
        if (!_records.TryGetValue(scopedKey, out IdempotencyRecord? record))
        {
            return null;
        }

        if (now - record.CreatedAt > Ttl)
        {
            _records.TryRemove(scopedKey, out _);
            return null;
        }

        return record;
    }

    /// <inheritdoc />
    public void Save(string scopedKey, IdempotencyRecord record)
        => _records.TryAdd(scopedKey, record);
}
