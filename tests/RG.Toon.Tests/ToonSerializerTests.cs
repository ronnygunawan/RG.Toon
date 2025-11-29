using System.Text.Json;

namespace RG.Toon.Tests;

public class ToonSerializerTests
{
    #region Primitive Serialization Tests

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("Ada_99", "Ada_99")]
    [InlineData("café", "café")]
    [InlineData("你好", "你好")]
    [InlineData("🚀", "🚀")]
    [InlineData("hello 👋 world", "hello 👋 world")]
    public void Serialize_SafeStrings_NoQuotes(string input, string expected)
    {
        var result = ToonSerializer.Serialize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("true", "\"true\"")]
    [InlineData("false", "\"false\"")]
    [InlineData("null", "\"null\"")]
    [InlineData("42", "\"42\"")]
    [InlineData("-3.14", "\"-3.14\"")]
    [InlineData("1e-6", "\"1e-6\"")]
    [InlineData("05", "\"05\"")]
    [InlineData("[test]", "\"[test]\"")]
    [InlineData("{key}", "\"{key}\"")]
    [InlineData("-", "\"-\"")]
    [InlineData("- item", "\"- item\"")]
    public void Serialize_StringsRequiringQuotes_AreQuoted(string input, string expected)
    {
        var result = ToonSerializer.Serialize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("line1\nline2", "\"line1\\nline2\"")]
    [InlineData("tab\there", "\"tab\\there\"")]
    [InlineData("return\rcarriage", "\"return\\rcarriage\"")]
    [InlineData("C:\\Users\\path", "\"C:\\\\Users\\\\path\"")]
    public void Serialize_StringsWithControlChars_AreEscaped(string input, string expected)
    {
        var result = ToonSerializer.Serialize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_IntegerNumber_ReturnsDecimal()
    {
        Assert.Equal("42", ToonSerializer.Serialize(42));
        Assert.Equal("-7", ToonSerializer.Serialize(-7));
        Assert.Equal("0", ToonSerializer.Serialize(0));
    }

    [Fact]
    public void Serialize_DecimalNumbers_ReturnsDecimal()
    {
        Assert.Equal("3.14", ToonSerializer.Serialize(3.14));
    }

    [Fact]
    public void Serialize_NegativeZero_ReturnsZero()
    {
        Assert.Equal("0", ToonSerializer.Serialize(-0.0));
    }

    [Fact]
    public void Serialize_LargeNumber_ReturnsDecimalNotation()
    {
        Assert.Equal("1000000", ToonSerializer.Serialize(1000000));
    }

    [Fact]
    public void Serialize_Booleans_ReturnsLowercase()
    {
        Assert.Equal("true", ToonSerializer.Serialize(true));
        Assert.Equal("false", ToonSerializer.Serialize(false));
    }

    [Fact]
    public void Serialize_Null_ReturnsNull()
    {
        Assert.Equal("null", ToonSerializer.Serialize<object?>(null));
    }

    [Fact]
    public void Serialize_SpecialFloats_ReturnNull()
    {
        Assert.Equal("null", ToonSerializer.Serialize(double.NaN));
        Assert.Equal("null", ToonSerializer.Serialize(double.PositiveInfinity));
        Assert.Equal("null", ToonSerializer.Serialize(double.NegativeInfinity));
        Assert.Equal("null", ToonSerializer.Serialize(float.NaN));
        Assert.Equal("null", ToonSerializer.Serialize(float.PositiveInfinity));
    }

    #endregion

    #region Object Serialization Tests

    public record SimpleObject
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public bool Active { get; init; }
    }

    [Fact]
    public void Serialize_SimpleObject_ReturnsKeyValuePairs()
    {
        var obj = new SimpleObject { Id = 123, Name = "Ada", Active = true };
        var result = ToonSerializer.Serialize(obj);
        Assert.Equal("Id: 123\nName: Ada\nActive: true", result);
    }

    public record NestedObject
    {
        public int Id { get; init; }
        public AddressInfo? Address { get; init; }
    }

    public record AddressInfo
    {
        public string City { get; init; } = "";
        public string Street { get; init; } = "";
    }

    [Fact]
    public void Serialize_NestedObject_UsesIndentation()
    {
        var obj = new NestedObject
        {
            Id = 1,
            Address = new AddressInfo { City = "Boulder", Street = "Main St" }
        };
        var result = ToonSerializer.Serialize(obj);
        var expected = "Id: 1\nAddress:\n  City: Boulder\n  Street: Main St";
        Assert.Equal(expected, result);
    }

    #endregion

    #region Array Serialization Tests

    [Fact]
    public void Serialize_PrimitiveArray_ReturnsInlineFormat()
    {
        var arr = new[] { "a", "b", "c" };
        var result = ToonSerializer.Serialize(arr);
        Assert.Equal("[3]: a,b,c", result);
    }

    [Fact]
    public void Serialize_IntArray_ReturnsInlineFormat()
    {
        var arr = new[] { 1, 2, 3 };
        var result = ToonSerializer.Serialize(arr);
        Assert.Equal("[3]: 1,2,3", result);
    }

    [Fact]
    public void Serialize_EmptyArray_ReturnsEmptyArrayFormat()
    {
        var arr = Array.Empty<int>();
        var result = ToonSerializer.Serialize(arr);
        Assert.Equal("[0]:", result);
    }

    [Fact]
    public void Serialize_ObjectWithArray_ReturnsInlineFormat()
    {
        var obj = new { Tags = new[] { "admin", "ops", "dev" } };
        var result = ToonSerializer.Serialize(obj);
        Assert.Equal("Tags[3]: admin,ops,dev", result);
    }

    public record TabularItem
    {
        public string Sku { get; init; } = "";
        public int Qty { get; init; }
        public decimal Price { get; init; }
    }

    [Fact]
    public void Serialize_TabularArray_ReturnsTabularFormat()
    {
        var items = new[]
        {
            new TabularItem { Sku = "A1", Qty = 2, Price = 9.99m },
            new TabularItem { Sku = "B2", Qty = 1, Price = 14.5m }
        };
        var obj = new { Items = items };
        var result = ToonSerializer.Serialize(obj);
        var expected = "Items[2]{Sku,Qty,Price}:\n  A1,2,9.99\n  B2,1,14.5";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_TabularArrayWithQuotedValues_QuotesDelimiters()
    {
        var items = new[]
        {
            new { Sku = "A,1", Desc = "cool", Qty = 2 },
            new { Sku = "B2", Desc = "wip: test", Qty = 1 }
        };
        var obj = new { Items = items };
        var result = ToonSerializer.Serialize(obj);
        Assert.Contains("\"A,1\"", result);
        Assert.Contains("\"wip: test\"", result);
    }

    #endregion

    #region Attribute Tests

    public record AttributeTestObject
    {
        [ToonPropertyName("name")]
        public string PersonName { get; init; } = "";

        [ToonIgnore]
        public string NormalizedName => PersonName.ToUpperInvariant();

        public int Age { get; init; }
    }

    [Fact]
    public void Serialize_ToonPropertyName_UsesCustomName()
    {
        var obj = new AttributeTestObject { PersonName = "Ada", Age = 30 };
        var result = ToonSerializer.Serialize(obj);
        Assert.Contains("name: Ada", result);
        Assert.DoesNotContain("PersonName", result);
    }

    [Fact]
    public void Serialize_ToonIgnore_ExcludesProperty()
    {
        var obj = new AttributeTestObject { PersonName = "Ada", Age = 30 };
        var result = ToonSerializer.Serialize(obj);
        Assert.DoesNotContain("NormalizedName", result);
        Assert.DoesNotContain("ADA", result);
    }

    #endregion

    #region Deserialization Tests

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("Ada_99", "Ada_99")]
    [InlineData("\"\"", "")]
    [InlineData("\"true\"", "true")]
    [InlineData("\"42\"", "42")]
    public void Deserialize_Strings_ReturnsCorrectValue(string input, string expected)
    {
        var result = ToonSerializer.Deserialize<string>(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Deserialize_Integer_ReturnsCorrectValue()
    {
        Assert.Equal(42, ToonSerializer.Deserialize<int>("42"));
        Assert.Equal(-7, ToonSerializer.Deserialize<int>("-7"));
        Assert.Equal(0, ToonSerializer.Deserialize<int>("0"));
    }

    [Fact]
    public void Deserialize_Decimal_ReturnsCorrectValue()
    {
        Assert.Equal(3.14, ToonSerializer.Deserialize<double>("3.14"));
    }

    [Fact]
    public void Deserialize_Boolean_ReturnsCorrectValue()
    {
        Assert.True(ToonSerializer.Deserialize<bool>("true"));
        Assert.False(ToonSerializer.Deserialize<bool>("false"));
    }

    [Fact]
    public void Deserialize_SimpleObject_ReturnsCorrectObject()
    {
        var toon = "Id: 123\nName: Ada\nActive: true";
        var result = ToonSerializer.Deserialize<SimpleObject>(toon);
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Ada", result.Name);
        Assert.True(result.Active);
    }

    [Fact]
    public void Deserialize_NestedObject_ReturnsCorrectObject()
    {
        var toon = "Id: 1\nAddress:\n  City: Boulder\n  Street: Main St";
        var result = ToonSerializer.Deserialize<NestedObject>(toon);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Address);
        Assert.Equal("Boulder", result.Address.City);
        Assert.Equal("Main St", result.Address.Street);
    }

    [Fact]
    public void Deserialize_InlineArray_ReturnsCorrectArray()
    {
        var toon = "[3]: a,b,c";
        var result = ToonSerializer.Deserialize<string[]>(toon);
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Deserialize_IntArray_ReturnsCorrectArray()
    {
        var toon = "[3]: 1,2,3";
        var result = ToonSerializer.Deserialize<int[]>(toon);
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void Deserialize_TabularArray_ReturnsCorrectObjects()
    {
        var toon = "[2]{Sku,Qty,Price}:\n  A1,2,9.99\n  B2,1,14.5";
        var result = ToonSerializer.Deserialize<TabularItem[]>(toon);
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("A1", result[0].Sku);
        Assert.Equal(2, result[0].Qty);
        Assert.Equal(9.99m, result[0].Price);
        Assert.Equal("B2", result[1].Sku);
        Assert.Equal(1, result[1].Qty);
        Assert.Equal(14.5m, result[1].Price);
    }

    [Fact]
    public void Deserialize_ObjectWithArray_ReturnsCorrectObject()
    {
        var toon = "Tags[3]: admin,ops,dev";
        var result = ToonSerializer.Deserialize<TagsObject>(toon);
        Assert.NotNull(result);
        Assert.NotNull(result.Tags);
        Assert.Equal(3, result.Tags.Length);
        Assert.Equal(["admin", "ops", "dev"], result.Tags);
    }

    public record TagsObject
    {
        public string[] Tags { get; init; } = [];
    }

    [Fact]
    public void Deserialize_WithToonPropertyName_UsesCustomName()
    {
        var toon = "name: Ada\nAge: 30";
        var result = ToonSerializer.Deserialize<AttributeTestObject>(toon);
        Assert.NotNull(result);
        Assert.Equal("Ada", result.PersonName);
        Assert.Equal(30, result.Age);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_SimpleObject()
    {
        var original = new SimpleObject { Id = 42, Name = "Test", Active = true };
        var toon = ToonSerializer.Serialize(original);
        var result = ToonSerializer.Deserialize<SimpleObject>(toon);
        Assert.Equal(original.Id, result?.Id);
        Assert.Equal(original.Name, result?.Name);
        Assert.Equal(original.Active, result?.Active);
    }

    [Fact]
    public void RoundTrip_NestedObject()
    {
        var original = new NestedObject
        {
            Id = 1,
            Address = new AddressInfo { City = "Boulder", Street = "Main St" }
        };
        var toon = ToonSerializer.Serialize(original);
        var result = ToonSerializer.Deserialize<NestedObject>(toon);
        Assert.Equal(original.Id, result?.Id);
        Assert.Equal(original.Address?.City, result?.Address?.City);
        Assert.Equal(original.Address?.Street, result?.Address?.Street);
    }

    [Fact]
    public void RoundTrip_TabularArray()
    {
        var original = new[]
        {
            new TabularItem { Sku = "A1", Qty = 2, Price = 9.99m },
            new TabularItem { Sku = "B2", Qty = 1, Price = 14.5m }
        };
        var toon = ToonSerializer.Serialize(original);
        var result = ToonSerializer.Deserialize<TabularItem[]>(toon);
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(original[0].Sku, result[0].Sku);
        Assert.Equal(original[0].Qty, result[0].Qty);
        Assert.Equal(original[0].Price, result[0].Price);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Serialize_EmptyObject_ReturnsEmptyString()
    {
        var result = ToonSerializer.Serialize(new { });
        Assert.Equal("", result);
    }

    [Fact]
    public void Serialize_ObjectWithNullValues()
    {
        var obj = new { Name = (string?)null, Age = 25 };
        var result = ToonSerializer.Serialize(obj);
        Assert.Contains("Name: null", result);
        Assert.Contains("Age: 25", result);
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        var result = ToonSerializer.Deserialize<SimpleObject>("");
        Assert.Null(result);
    }

    [Fact]
    public void Serialize_QuotedKeys()
    {
        var obj = new QuotedKeyObject { OrderId = 1, FullName = "Ada" };
        var result = ToonSerializer.Serialize(obj);
        Assert.Contains("\"order:id\": 1", result);
        Assert.Contains("\"full name\": Ada", result);
    }

    public record QuotedKeyObject
    {
        [ToonPropertyName("order:id")]
        public int OrderId { get; init; }

        [ToonPropertyName("full name")]
        public string FullName { get; init; } = "";
    }

    [Fact]
    public void Deserialize_QuotedKeys()
    {
        var toon = "\"order:id\": 1\n\"full name\": Ada";
        var result = ToonSerializer.Deserialize<QuotedKeyObject>(toon);
        Assert.NotNull(result);
        Assert.Equal(1, result.OrderId);
        Assert.Equal("Ada", result.FullName);
    }

    [Fact]
    public void Serialize_EscapedStrings()
    {
        var obj = new { Path = "C:\\Users\\test", Text = "line1\nline2" };
        var result = ToonSerializer.Serialize(obj);
        Assert.Contains("\"C:\\\\Users\\\\test\"", result);
        Assert.Contains("\"line1\\nline2\"", result);
    }

    [Fact]
    public void Deserialize_EscapedStrings()
    {
        var toon = "Path: \"C:\\\\Users\\\\test\"\nText: \"line1\\nline2\"";
        var result = ToonSerializer.Deserialize<PathObject>(toon);
        Assert.NotNull(result);
        Assert.Equal("C:\\Users\\test", result.Path);
        Assert.Equal("line1\nline2", result.Text);
    }

    public record PathObject
    {
        public string Path { get; init; } = "";
        public string Text { get; init; } = "";
    }

    #endregion

    #region Complex Scenarios

    public record ComplexObject
    {
        public string Task { get; init; } = "";
        public string Location { get; init; } = "";
        public string[] Friends { get; init; } = [];
        public HikeInfo[] Hikes { get; init; } = [];
    }

    public record HikeInfo
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public double DistanceKm { get; init; }
        public int ElevationGain { get; init; }
        public string Companion { get; init; } = "";
        public bool WasSunny { get; init; }
    }

    [Fact]
    public void Serialize_ComplexObject_ProducesValidToon()
    {
        var obj = new ComplexObject
        {
            Task = "Our favorite hikes together",
            Location = "Boulder",
            Friends = ["ana", "luis", "sam"],
            Hikes =
            [
                new HikeInfo { Id = 1, Name = "Blue Lake Trail", DistanceKm = 7.5, ElevationGain = 320, Companion = "ana", WasSunny = true },
                new HikeInfo { Id = 2, Name = "Ridge Overlook", DistanceKm = 9.2, ElevationGain = 540, Companion = "luis", WasSunny = false },
                new HikeInfo { Id = 3, Name = "Wildflower Loop", DistanceKm = 5.1, ElevationGain = 180, Companion = "sam", WasSunny = true }
            ]
        };

        var result = ToonSerializer.Serialize(obj);

        Assert.Contains("Task: Our favorite hikes together", result);
        Assert.Contains("Location: Boulder", result);
        Assert.Contains("Friends[3]: ana,luis,sam", result);
        Assert.Contains("Hikes[3]{Id,Name,DistanceKm,ElevationGain,Companion,WasSunny}:", result);
        Assert.Contains("1,Blue Lake Trail,7.5,320,ana,true", result);
        Assert.Contains("2,Ridge Overlook,9.2,540,luis,false", result);
        Assert.Contains("3,Wildflower Loop,5.1,180,sam,true", result);
    }

    [Fact]
    public void RoundTrip_ComplexObject()
    {
        var original = new ComplexObject
        {
            Task = "Our favorite hikes together",
            Location = "Boulder",
            Friends = ["ana", "luis", "sam"],
            Hikes =
            [
                new HikeInfo { Id = 1, Name = "Blue Lake Trail", DistanceKm = 7.5, ElevationGain = 320, Companion = "ana", WasSunny = true },
                new HikeInfo { Id = 2, Name = "Ridge Overlook", DistanceKm = 9.2, ElevationGain = 540, Companion = "luis", WasSunny = false }
            ]
        };

        var toon = ToonSerializer.Serialize(original);
        var result = ToonSerializer.Deserialize<ComplexObject>(toon);

        Assert.NotNull(result);
        Assert.Equal(original.Task, result.Task);
        Assert.Equal(original.Location, result.Location);
        Assert.Equal(original.Friends, result.Friends);
        Assert.Equal(original.Hikes.Length, result.Hikes.Length);
        Assert.Equal(original.Hikes[0].Name, result.Hikes[0].Name);
        Assert.Equal(original.Hikes[0].DistanceKm, result.Hikes[0].DistanceKm);
        Assert.Equal(original.Hikes[0].WasSunny, result.Hikes[0].WasSunny);
    }

    #endregion
}
