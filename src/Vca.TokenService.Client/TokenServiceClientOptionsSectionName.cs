namespace Vca.TokenService.Client;

internal static class TokenServiceClientOptionsSectionName
{
    public static string For<T>() where T : TokenServiceClientOptions
    {
        var type = typeof(T);
        var attribute = (TokenServiceClientOptionsSectionNameAttribute?)Attribute.GetCustomAttribute(type, typeof(TokenServiceClientOptionsSectionNameAttribute));

        return attribute != null
            ? attribute.Name
            : type.Name;
    }
}
