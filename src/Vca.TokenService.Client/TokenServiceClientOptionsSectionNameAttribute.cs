namespace Vca.TokenService.Client;

/// <summary>
/// Attribute to specify the configuration section name for a TokenServiceOptions subclass.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TokenServiceClientOptionsSectionNameAttribute : Attribute
{
    internal static readonly string ErrorMessageMissingSectionName = "Name cannot be null, empty, or whitespace. Please provide a valid section name.";
    internal static readonly string ErrorMessageInvalidSectionName = "Invalid section name TokenService. Use a different section name.";

    private static readonly string DefaultSectionName = "TokenService";

    /// <summary>
    /// Gets the name of the configuration section.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceClientOptionsSectionNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the configuration section.</param>
    public TokenServiceClientOptionsSectionNameAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(ErrorMessageMissingSectionName, nameof(name));
        
        if (string.Equals(name, DefaultSectionName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(ErrorMessageInvalidSectionName, nameof(name));

        Name = name;
    }

    internal TokenServiceClientOptionsSectionNameAttribute()
    {
        Name = "TokenService";
    }
}
