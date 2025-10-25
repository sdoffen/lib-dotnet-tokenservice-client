using System.Text;

namespace Vca.TokenService.Client.Tests;

public sealed class TokenServiceOptionsTests
{
    [Fact]
    public void EncodedAuthToken_ReturnsBase64_WhenBasicAuthSet()
    {
        var opts = new TestOptions { BasicAuth = "user:pass" };

        var expected = Convert.ToBase64String(Encoding.ASCII.GetBytes("user:pass"));
        opts.EncodedAuthToken.ShouldBe(expected);
    }

    [Fact]
    public void EncodedAuthToken_ThrowsArgumentNullException_WhenBasicAuthIsNull()
    {
        var opts = new TestOptions();
        opts.BasicAuth.ShouldBeNull();

        Should.Throw<ArgumentNullException>(() => _ = opts.EncodedAuthToken)
              .ParamName.ShouldBe("s"); // parameter name for ASCIIEncoding.GetBytes(string s)
    }

    [Fact]
    public void EncodedAuthToken_ReturnsEmptyString_WhenBasicAuthIsEmpty()
    {
        var opts = new TestOptions { BasicAuth = string.Empty };

        // Base64 of empty byte[] is empty string
        opts.EncodedAuthToken.ShouldBe(string.Empty);
    }

    [Fact]
    public void EncodedAuthToken_Updates_WhenBasicAuthChanges()
    {
        var first = "alpha:one";
        var second = "beta:two";
        first.ShouldNotBe(second);

        var encodedFirst = Convert.ToBase64String(Encoding.ASCII.GetBytes(first));
        var encodedSecond = Convert.ToBase64String(Encoding.ASCII.GetBytes(second));
        encodedFirst.ShouldNotBe(encodedSecond);

        var opts = new TestOptions { BasicAuth = first };
        opts.EncodedAuthToken.ShouldBe(encodedFirst);

        opts.BasicAuth = second;
        opts.EncodedAuthToken.ShouldBe(encodedSecond);
    }

    [Fact]
    public void ServiceUrl_GetsAndSets_Value()
    {
        var opts = new TestOptions { ServiceUrl = "https://example/token" };
        opts.ServiceUrl.ShouldBe("https://example/token");
    }

    [Fact]
    public void ClientName_IsPrefixed_WithGuidSuffix_AndStableWithinAppDomain()
    {
        var value1 = TokenServiceClientOptions.ClientName;
        var value2 = TokenServiceClientOptions.ClientName;

        value1.ShouldNotBeNull();
        value2.ShouldNotBeNull();

        value1.ShouldBe(value2); // static value initialized once
        value1.ShouldStartWith("TokenServiceClient-");

        var suffix = value1["TokenServiceClient-".Length..];
        Guid.TryParse(suffix, out _).ShouldBeTrue();
    }

    private sealed class TestOptions : TokenServiceClientOptions { }
}
