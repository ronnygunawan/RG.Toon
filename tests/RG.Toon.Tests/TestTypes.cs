using RG.Toon;
using System.Text.Json.Serialization;

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

public class PersonWithJsonAttributes
{
    public int Id { get; set; }
    
    [JsonPropertyName("full_name")]
    public string Name { get; set; } = "";
    
    [JsonIgnore]
    public string InternalId { get; set; } = "";
    
    public int Age { get; set; }
}

public class PersonWithMixedAttributes
{
    public int Id { get; set; }
    
    [JsonPropertyName("json_name")]
    public string Name { get; set; } = "";
    
    [ToonIgnore]
    public string ToonIgnoredField { get; set; } = "";
    
    [JsonIgnore]
    public string JsonIgnoredField { get; set; } = "";
    
    public int Age { get; set; }
}

public class PersonWithBothPropertyNameAttributes
{
    public int Id { get; set; }
    
    // ToonPropertyName should take precedence over JsonPropertyName
    [ToonPropertyName("toon_name")]
    [JsonPropertyName("json_name")]
    public string Name { get; set; } = "";
    
    public int Age { get; set; }
}
