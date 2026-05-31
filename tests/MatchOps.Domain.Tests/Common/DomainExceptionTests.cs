using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Common;

public class DomainExceptionTests
{
    [Fact]
    public void Ctor_Default_HasNonEmptyMessage()
    {
        var ex = new DomainException();

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void Ctor_WithMessage_SetsMessage()
    {
        var ex = new DomainException("不変条件違反");

        Assert.Equal("不変条件違反", ex.Message);
    }

    [Fact]
    public void Ctor_WithInnerException_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("原因");

        var ex = new DomainException("不変条件違反", inner);

        Assert.Equal("不変条件違反", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
