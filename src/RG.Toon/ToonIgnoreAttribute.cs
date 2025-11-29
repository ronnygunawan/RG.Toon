namespace RG.Toon;

/// <summary>
/// Indicates that the property should be ignored during serialization and deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ToonIgnoreAttribute : Attribute
{
}
