// -----------------------------------------------------------------------------
// <copyright file="HoldoutAssignmentPolicy.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト割当の決定的ポリシー（ADR-0007）。実験 ID と顧客 ID から安定ハッシュで [0,1) の一様値を導き、
// control 比率と比較して arm を決める。シードや乱数を用いず、同一入力なら常に同一 arm（再現可能・テスト容易）。
// 顧客ごとに安定するため、再実行や再集計でも割当が揺れない。
// </summary>
// -----------------------------------------------------------------------------

using System.Security.Cryptography;
using MatchOps.Domain.Common;

namespace MatchOps.Domain.Experiments;

/// <summary>決定的ハッシュによるホールドアウト割当ポリシー。</summary>
public sealed class HoldoutAssignmentPolicy
{
    /// <summary>
    /// 実験 ID と顧客 ID から arm を決定的に割り当てる。
    /// </summary>
    /// <param name="experimentId">実験 ID。</param>
    /// <param name="customerId">顧客 ID。</param>
    /// <param name="controlRatio">対照群の比率（0〜1。0 で全 treatment、1 で全 control）。</param>
    /// <returns>割り当てられた <see cref="ExperimentArm"/>。</returns>
    /// <exception cref="DomainException"><paramref name="controlRatio"/> が 0〜1 の範囲外・非数の場合。</exception>
    public ExperimentArm Assign(ExperimentId experimentId, CustomerId customerId, double controlRatio)
    {
        if (double.IsNaN(controlRatio) || controlRatio < 0d || controlRatio > 1d)
        {
            throw new DomainException("対照群比率は 0〜1 で指定してください。");
        }

        double unit = DeterministicUnit(experimentId, customerId);
        return unit < controlRatio ? ExperimentArm.Control : ExperimentArm.Treatment;
    }

    private static double DeterministicUnit(ExperimentId experimentId, CustomerId customerId)
    {
        Span<byte> input = stackalloc byte[32];
        experimentId.Value.TryWriteBytes(input[..16]);
        customerId.Value.TryWriteBytes(input[16..]);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);

        // 先頭 8 バイトを符号なし整数として取り出し、[0,1) に正規化する。
        ulong value = BitConverter.ToUInt64(hash[..8]);
        return value / (ulong.MaxValue + 1.0d);
    }
}
