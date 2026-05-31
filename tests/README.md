# tests/ — テストプロジェクト

```
tests/
├── MatchOps.Domain.Tests/         # Domain 単体（DB 不要）
├── MatchOps.Application.Tests/    # Application 単体（Mock）
├── MatchOps.Infrastructure.Tests/ # EF Core 統合（Testcontainers PostgreSQL）
├── MatchOps.Api.Tests/            # HTTP（WebApplicationFactory）
└── MatchOps.IntegrationTests/     # E2E（Testcontainers + WireMock.Net）
```

規約は `CLAUDE.md` §8 / `AGENTS.md` §2。テストファースト・カバレッジ目標（全体 85%+）。
