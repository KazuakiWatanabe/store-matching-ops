using System.Reflection;
using MatchOps.Application.Notifications;
using MatchOps.Infrastructure.Persistence;

namespace MatchOps.Infrastructure.Tests.Notifications;

/// <summary>
/// 配信系の型に連絡先（PII）フィールドが存在しないことを型レベルで保証する（CLAUDE.md §9.2）。
/// 配信メッセージ・配信ログは顧客を CustomerId で参照し、メール/電話/住所等を平文で保持しない。
/// </summary>
public sealed class NotificationPiiBoundaryTests
{
    public static TheoryData<Type> NotificationTypes =>
    [
        typeof(NotificationMessage),
        typeof(OutboxMessageEntity),
        typeof(NotificationLogEntry),
    ];

    private static readonly string[] ForbiddenNameFragments =
        ["email", "phone", "address", "contact"];

    [Theory]
    [MemberData(nameof(NotificationTypes))]
    public void NotificationType_HasNoContactField(Type type)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string lowered = property.Name.ToLowerInvariant();
            foreach (string fragment in ForbiddenNameFragments)
            {
                Assert.False(
                    lowered.Contains(fragment, StringComparison.Ordinal),
                    $"{type.Name}.{property.Name} が連絡先 PII を示唆する名前を含んでいます。");
            }
        }
    }
}
