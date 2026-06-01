using System.Reflection;
using MatchOps.Application.Ai;

namespace MatchOps.Infrastructure.Tests.Ai;

/// <summary>
/// AI 入力 DTO に PII・識別子フィールドが存在しないことを型レベルで保証する（ADR-0005）。
/// 強い型 ID（MatchOps.Domain.Common）や連絡先を示す名前のプロパティを持たないことを反射で検証する。
/// </summary>
public sealed class AiContextPiiBoundaryTests
{
    public static TheoryData<Type> AiInputTypes =>
    [
        typeof(AiCampaignContext),
        typeof(AiMessageContext),
        typeof(AiResultContext),
        typeof(AllowedOffer),
    ];

    private static readonly string[] ForbiddenNameFragments =
        ["email", "phone", "address", "contact", "customerid", "firstname", "lastname", "fullname"];

    [Theory]
    [MemberData(nameof(AiInputTypes))]
    public void AiInputDto_HasNoIdentifierOrContactFields(Type type)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // 強い型 ID（Domain.Common 名前空間の型）を入力に持ち込まない。
            string? propertyNamespace = property.PropertyType.Namespace;
            Assert.False(
                propertyNamespace is not null && propertyNamespace.StartsWith("MatchOps.Domain", StringComparison.Ordinal),
                $"{type.Name}.{property.Name} が Domain 型 ({property.PropertyType.Name}) を含んでいます。");

            // 連絡先・個人識別を示す名前を持たない。
            string lowered = property.Name.ToLowerInvariant();
            foreach (string fragment in ForbiddenNameFragments)
            {
                Assert.False(
                    lowered.Contains(fragment, StringComparison.Ordinal),
                    $"{type.Name}.{property.Name} が PII を示唆する名前を含んでいます。");
            }
        }
    }
}
