using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;

namespace MatchOps.Domain.Tests.Customers;

public class ContactHashTests
{
    // SHA-256("example") を表す 64 桁の 16 進文字列。
    private const string ValidSha256Hex = "50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c";

    [Fact]
    public void From_ValidSha256Hex_CreatesHash()
    {
        var hash = ContactHash.From(ValidSha256Hex);

        Assert.Equal(ValidSha256Hex, hash.Value);
    }

    [Fact]
    public void ToString_ReturnsHashValue()
    {
        Assert.Equal(ValidSha256Hex, ContactHash.From(ValidSha256Hex).ToString());
    }

    [Fact]
    public void From_UppercaseHex_IsNormalizedToLowercase()
    {
        var hash = ContactHash.From(ValidSha256Hex.ToUpperInvariant());

        Assert.Equal(ValidSha256Hex, hash.Value);
    }

    [Theory]
    [InlineData("090-1234-5678")]                 // 平文の電話番号
    [InlineData("user@example.com")]              // 平文のメールアドレス
    [InlineData("")]                              // 空
    [InlineData("abc123")]                        // 短すぎる
    [InlineData("50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545")]   // 63 桁
    [InlineData("50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545cz")] // 非 16 進
    public void From_PlaintextOrInvalid_Throws(string input)
    {
        Assert.Throws<DomainException>(() => ContactHash.From(input));
    }

    [Fact]
    public void From_Plaintext_ExceptionMessageDoesNotLeakInput()
    {
        const string plaintextEmail = "user@example.com";

        var ex = Assert.Throws<DomainException>(() => ContactHash.From(plaintextEmail));

        // 平文 PII を例外メッセージに含めない。
        Assert.DoesNotContain(plaintextEmail, ex.Message);
    }
}
