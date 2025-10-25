namespace Vca.TokenService.Client.Tests;

public sealed class TokenServiceOptionsSectionNameAttributeTests
{
    [Theory]
    [InlineData("MySection")]
    [InlineData("Token-Service-Prod")]
    [InlineData("token-service-dev")]
    public void Ctor_SetsName_WhenValidNonDefault(string section)
    {
        var attr = new TokenServiceClientOptionsSectionNameAttribute(section);

        attr.Name.ShouldBe(section);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Ctor_ThrowsArgumentException_WhenNameMissing(string? section)
    {
        var ex = Should.Throw<ArgumentException>(() => new TokenServiceClientOptionsSectionNameAttribute(section!));

        ex.ParamName.ShouldBe("name");
        ex.Message.ShouldStartWith(TokenServiceClientOptionsSectionNameAttribute.ErrorMessageMissingSectionName);
    }

    [Theory]
    [InlineData("TokenService")]
    [InlineData("tokenservice")]
    [InlineData("TOKENSERVICE")]
    public void Ctor_ThrowsArgumentException_WhenNameEqualsDefault_IgnoringCase(string section)
    {
        var ex = Should.Throw<ArgumentException>(() => new TokenServiceClientOptionsSectionNameAttribute(section));

        ex.ParamName.ShouldBe("name");
        ex.Message.ShouldStartWith(TokenServiceClientOptionsSectionNameAttribute.ErrorMessageInvalidSectionName);
    }

    [Fact]
    public void ParameterlessInternalCtor_SetsName_ToDefaultTokenService()
    {
        // The constructor is internal; use reflection to instantiate it.
        var instance = new TokenServiceClientOptionsSectionNameAttribute();
        instance.ShouldNotBeNull();

        instance.Name.ShouldNotBeNull();
        instance.Name.ShouldBe("TokenService");
    }

    [Fact]
    public void AttributeUsage_IsClassOnly_NotInherited_NotAllowMultiple()
    {
        var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(TokenServiceClientOptionsSectionNameAttribute),
            typeof(AttributeUsageAttribute));

        usage.ShouldNotBeNull();
        usage!.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.Inherited.ShouldBeFalse();
        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass_AndExposesName()
    {
        var targetType = typeof(DummyOptionsWithSectionName);
        var attr = targetType
            .GetCustomAttributes(typeof(TokenServiceClientOptionsSectionNameAttribute), inherit: false)
            .Cast<TokenServiceClientOptionsSectionNameAttribute>()
            .SingleOrDefault();

        attr.ShouldNotBeNull();
        attr!.Name.ShouldBe("MyCustomSection");
    }

    [TokenServiceClientOptionsSectionName("MyCustomSection")]
    private sealed class DummyOptionsWithSectionName { }
}
