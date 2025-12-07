using RG.Toon;

namespace RG.Toon.Tests;

[ToonSerializable]
public class SimplePerson
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

[ToonSerializable]
public class PersonWithNullableProperties
{
    public int? NullableInt { get; set; }
    public string? NullableString { get; set; }
    public bool? NullableBool { get; set; }
}

[ToonSerializable]
public class PersonWithVariousTypes
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public bool IsActive { get; set; }
    public decimal Balance { get; set; }
}

[ToonSerializable]
public class PersonWithAttributes
{
    public int Id { get; set; }
    
    [ToonPropertyName("full_name")]
    public string Name { get; set; } = "";
    
    [ToonIgnore]
    public string InternalId { get; set; } = "";
    
    public int Age { get; set; }
}
