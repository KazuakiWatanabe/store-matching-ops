// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaign.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策 (MatchingCampaign) Aggregate Root。1つの空き枠群に対する候補抽出〜提案〜承認〜配信の単位。
// 状態遷移: draft → scored → proposed → approved → sent → measured。
// proposed → approved は人手のみ（Approve(approvedBy, now)）。approved を経ずに Send 不可（ADR-0004）。
// 不正遷移は DomainException。AI/配信/永続化は持ち込まない（状態と候補の保持まで）。
// 関連: ADR-0004 (承認フロー), ADR-0003 (スコアリング)
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Domain.Matching;

/// <summary>施策を表す Aggregate Root。</summary>
public sealed class MatchingCampaign
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private List<TimeSlotId> _targetSlots;
    private List<MatchingCandidate> _candidates = [];

    private MatchingCampaign(CampaignId id, TenantId tenantId, StoreId storeId, List<TimeSlotId> targetSlots)
    {
        Id = id;
        TenantId = tenantId;
        StoreId = storeId;
        _targetSlots = targetSlots;
        Status = CampaignStatus.Draft;
    }

    // ORM（EF Core）による再構成専用のコンストラクタ。状態はバッキングフィールド/プロパティ経由で設定される。
    // 不変条件はファクトリ (Open) と状態遷移メソッドが担保し、本コンストラクタは新規生成には用いない。
    private MatchingCampaign() => _targetSlots = [];

    /// <summary>施策の一意識別子。</summary>
    public CampaignId Id { get; }

    /// <summary>所属テナント。</summary>
    public TenantId TenantId { get; }

    /// <summary>所属店舗。</summary>
    public StoreId StoreId { get; }

    /// <summary>対象の空き枠群。</summary>
    public IReadOnlyList<TimeSlotId> TargetSlots => _targetSlots;

    /// <summary>スコアリング済みの候補（スコア降順）。</summary>
    public IReadOnlyList<MatchingCandidate> Candidates => _candidates;

    /// <summary>現在の状態。</summary>
    public CampaignStatus Status { get; private set; }

    /// <summary>承認者（未承認なら <c>null</c>）。</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>承認日時（未承認なら <c>null</c>）。</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>配信日時（未配信なら <c>null</c>）。</summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>未ディスパッチのドメインイベント。</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// 対象の空き枠群を指定して施策を開く（draft）。
    /// </summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <param name="targetSlots">対象の空き枠群（1 件以上・重複は除去）。</param>
    /// <returns>draft 状態の <see cref="MatchingCampaign"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targetSlots"/> が <c>null</c> の場合。</exception>
    /// <exception cref="DomainException">対象の空き枠が 1 件もない場合。</exception>
    public static MatchingCampaign Open(TenantId tenantId, StoreId storeId, IEnumerable<TimeSlotId> targetSlots)
    {
        ArgumentNullException.ThrowIfNull(targetSlots);
        var slots = targetSlots.Distinct().ToList();
        if (slots.Count == 0)
        {
            throw new DomainException("対象の空き枠が指定されていません。");
        }

        return new MatchingCampaign(CampaignId.New(), tenantId, storeId, slots);
    }

    /// <summary>
    /// 候補のスコアリング結果を記録する（draft → scored）。
    /// </summary>
    /// <param name="candidates">スコアリング済み候補。</param>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> が <c>null</c> の場合。</exception>
    /// <exception cref="DomainException">draft 以外、または対象外の空き枠の候補が含まれる場合。</exception>
    public void RecordScoring(IReadOnlyCollection<MatchingCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        RequireStatus(CampaignStatus.Draft, "スコアリング");

        foreach (MatchingCandidate candidate in candidates)
        {
            if (!_targetSlots.Contains(candidate.TimeSlotId))
            {
                throw new DomainException("対象外の空き枠に対する候補は記録できません。");
            }
        }

        _candidates.AddRange(candidates);
        Status = CampaignStatus.Scored;
        _domainEvents.Add(new MatchScored(Id, _candidates.Count));
    }

    /// <summary>提案を行う（scored → proposed）。</summary>
    /// <exception cref="DomainException">scored 以外の状態の場合。</exception>
    public void Propose() => Transition(CampaignStatus.Scored, CampaignStatus.Proposed, "提案");

    /// <summary>
    /// 人手で承認する（proposed → approved, ADR-0004）。
    /// </summary>
    /// <param name="approvedBy">承認者。</param>
    /// <param name="now">承認日時。</param>
    /// <exception cref="DomainException">proposed 以外、または承認者が空白の場合。</exception>
    public void Approve(string approvedBy, DateTimeOffset now)
    {
        RequireStatus(CampaignStatus.Proposed, "承認");
        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new DomainException("承認者は必須です。");
        }

        ApprovedBy = approvedBy.Trim();
        ApprovedAt = now;
        Status = CampaignStatus.Approved;
        _domainEvents.Add(new CampaignApproved(Id, ApprovedBy, now));
    }

    /// <summary>
    /// 配信する（approved → sent）。承認を経ていなければ拒否する（ADR-0004）。
    /// </summary>
    /// <param name="now">配信日時。</param>
    /// <exception cref="DomainException">approved 以外の状態の場合。</exception>
    public void Send(DateTimeOffset now)
    {
        RequireStatus(CampaignStatus.Approved, "配信");
        SentAt = now;
        Status = CampaignStatus.Sent;
        _domainEvents.Add(new CampaignSent(Id, now));
    }

    /// <summary>効果測定済みにする（sent → measured）。</summary>
    /// <exception cref="DomainException">sent 以外の状態の場合。</exception>
    public void Measure() => Transition(CampaignStatus.Sent, CampaignStatus.Measured, "効果測定");

    /// <summary>ディスパッチ済みのドメインイベントをクリアする。</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Transition(CampaignStatus from, CampaignStatus to, string action)
    {
        RequireStatus(from, action);
        Status = to;
    }

    private void RequireStatus(CampaignStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException($"{Status} 状態の施策は{action}できません。");
        }
    }
}
