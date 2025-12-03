namespace RG.Toon;

/// <summary>
/// Specifies the property name that is present in the TOON output when serializing
/// and the property name used when deserializing.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ToonPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the property in TOON.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ToonPropertyNameAttribute"/> with the specified property name.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    public ToonPropertyNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
