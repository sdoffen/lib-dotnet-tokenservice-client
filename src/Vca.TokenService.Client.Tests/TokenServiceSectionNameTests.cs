namespace Vca.TokenService.Client.Tests;

public sealed class TokenServiceSectionNameTests
{
    [Fact]
    public void For_ReturnsSectionName_WhenAttributePresent()
    {
        var name = TokenServiceClientOptionsSectionName.For<OptionsWithAttribute>();
        name.ShouldBe("MyCustomSection");
    }

    [Fact]
    public void For_ReturnsClassName_WhenAttributeMissing()
    {
        var expected = typeof(OptionsWithoutAttribute).Name;

        var name = TokenServiceClientOptionsSectionName.For<OptionsWithoutAttribute>();

        name.ShouldBe(expected);
    }

    [Fact]
    public void For_ReturnsClassName_ForDerivedType_WhenBaseHasAttribute()
    {
        var expected = typeof(DerivedFromAttributed).Name;

        var name = TokenServiceClientOptionsSectionName.For<DerivedFromAttributed>();

        name.ShouldBe(expected);
    }

    [TokenServiceClientOptionsSectionName("MyCustomSection")]
    public class OptionsWithAttribute : TokenServiceClientOptions { }

    public class OptionsWithoutAttribute : TokenServiceClientOptions { }

    public class DerivedFromAttributed : OptionsWithAttribute { }
}
