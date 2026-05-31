// -----------------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ドメインイベントのマーカーインターフェース。
// Aggregate が発生させた事実（過去形）を表し、Application 層がディスパッチする。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>ドメインイベントを表すマーカーインターフェース。</summary>
public interface IDomainEvent;
