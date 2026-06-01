// -----------------------------------------------------------------------------
// <copyright file="IIdempotencyStore.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Idempotency-Key と保存済みレスポンスの対応を保持する抽象（CLAUDE.md §10.1）。
// Phase 0 はインメモリ実装。Phase 1 で Redis 等に置き換える。
// キーはテナントでスコープし、テナント跨ぎの衝突を防ぐ。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Idempotency;

/// <summary>冪等キーに対する保存済みレスポンスを管理する抽象。</summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// 保存済みレコードを取得する。期限切れ・未保存の場合は <c>null</c>。
    /// </summary>
    /// <param name="scopedKey">テナントでスコープした冪等キー。</param>
    /// <param name="now">現在時刻（期限判定に使用）。</param>
    /// <returns>保存済みレコード。なければ <c>null</c>。</returns>
    IdempotencyRecord? TryGet(string scopedKey, DateTimeOffset now);

    /// <summary>
    /// レコードを保存する。既存キーがあれば上書きしない（最初の確定を保持する）。
    /// </summary>
    /// <param name="scopedKey">テナントでスコープした冪等キー。</param>
    /// <param name="record">保存するレコード。</param>
    void Save(string scopedKey, IdempotencyRecord record);
}
