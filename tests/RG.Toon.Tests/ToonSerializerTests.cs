using Shouldly;

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
        result.ShouldBe(expected);
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
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("line1\nline2", "\"line1\\nline2\"")]
    [InlineData("tab\there", "\"tab\\there\"")]
    [InlineData("return\rcarriage", "\"return\\rcarriage\"")]
    [InlineData("C:\\Users\\path", "\"C:\\\\Users\\\\path\"")]
    public void Serialize_StringsWithControlChars_AreEscaped(string input, string expected)
    {
        var result = ToonSerializer.Serialize(input);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(-7, "-7")]
    [InlineData(0, "0")]
    public void Serialize_IntegerNumber_ReturnsDecimal(int input, string expected)
    {
        ToonSerializer.Serialize(input).ShouldBe(expected);
    }

    [Fact]
    public void Serialize_DecimalNumbers_ReturnsDecimal()
    {
        ToonSerializer.Serialize(3.14).ShouldBe("3.14");
    }

    [Fact]
    public void Serialize_NegativeZero_ReturnsZero()
    {
        ToonSerializer.Serialize(-0.0).ShouldBe("0");
    }

    [Fact]
    public void Serialize_LargeNumber_ReturnsDecimalNotation()
    {
        ToonSerializer.Serialize(1000000).ShouldBe("1000000");
    }

    [Fact]
    public void Serialize_Booleans_ReturnsLowercase()
    {
        ToonSerializer.Serialize(true).ShouldBe("true");
        ToonSerializer.Serialize(false).ShouldBe("false");
    }

    [Fact]
    public void Serialize_Null_ReturnsNull()
    {
        ToonSerializer.Serialize<object?>(null).ShouldBe("null");
    }

    [Fact]
    public void Serialize_SpecialFloats_ReturnNull()
    {
        ToonSerializer.Serialize(double.NaN).ShouldBe("null");
        ToonSerializer.Serialize(double.PositiveInfinity).ShouldBe("null");
        ToonSerializer.Serialize(double.NegativeInfinity).ShouldBe("null");
        ToonSerializer.Serialize(float.NaN).ShouldBe("null");
        ToonSerializer.Serialize(float.PositiveInfinity).ShouldBe("null");
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
        result.ShouldBe("Id: 123\nName: Ada\nActive: true");
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
        result.ShouldBe(expected);
    }

    #endregion

    #region Array Serialization Tests

    [Fact]
    public void Serialize_PrimitiveArray_ReturnsInlineFormat()
    {
        var arr = new[] { "a", "b", "c" };
        var result = ToonSerializer.Serialize(arr);
        result.ShouldBe("[3]: a,b,c");
    }

    [Fact]
    public void Serialize_IntArray_ReturnsInlineFormat()
    {
        var arr = new[] { 1, 2, 3 };
        var result = ToonSerializer.Serialize(arr);
        result.ShouldBe("[3]: 1,2,3");
    }

    [Fact]
    public void Serialize_EmptyArray_ReturnsEmptyArrayFormat()
    {
        var arr = Array.Empty<int>();
        var result = ToonSerializer.Serialize(arr);
        result.ShouldBe("[0]:");
    }

    [Fact]
    public void Serialize_ObjectWithArray_ReturnsInlineFormat()
    {
        var obj = new { Tags = new[] { "admin", "ops", "dev" } };
        var result = ToonSerializer.Serialize(obj);
        result.ShouldBe("Tags[3]: admin,ops,dev");
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
        result.ShouldBe(expected);
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
        result.ShouldContain("\"A,1\"");
        result.ShouldContain("\"wip: test\"");
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
        result.ShouldContain("name: Ada");
        result.ShouldNotContain("PersonName");
    }

    [Fact]
    public void Serialize_ToonIgnore_ExcludesProperty()
    {
        var obj = new AttributeTestObject { PersonName = "Ada", Age = 30 };
        var result = ToonSerializer.Serialize(obj);
        result.ShouldNotContain("NormalizedName");
        // NormalizedName would add "ADA" as the value, but since it's ignored, it shouldn't be there
        // We check for "ADA" case-sensitively as part of a key-value pair
        result.ShouldNotContain("NormalizedName: ADA");
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
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-7", -7)]
    [InlineData("0", 0)]
    public void Deserialize_Integer_ReturnsCorrectValue(string input, int expected)
    {
        ToonSerializer.Deserialize<int>(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("-3.14", -3.14)]
    [InlineData("1e-6", 0.000001)]
    [InlineData("-1E+9", -1000000000.0)]
    public void Deserialize_Decimal_ReturnsCorrectValue(string input, double expected)
    {
        ToonSerializer.Deserialize<double>(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("05")]
    [InlineData("0001")]
    [InlineData("007")]
    public void Deserialize_LeadingZeroTokens_TreatedAsStrings(string input)
    {
        // Tokens with leading zeros should be treated as strings, not numbers
        ToonSerializer.Deserialize<string>(input).ShouldBe(input);
    }

    [Fact]
    public void Deserialize_Boolean_ReturnsCorrectValue()
    {
        ToonSerializer.Deserialize<bool>("true").ShouldBeTrue();
        ToonSerializer.Deserialize<bool>("false").ShouldBeFalse();
    }

    [Fact]
    public void Deserialize_SimpleObject_ReturnsCorrectObject()
    {
        var toon = "Id: 123\nName: Ada\nActive: true";
        var result = ToonSerializer.Deserialize<SimpleObject>(toon);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(123);
        result.Name.ShouldBe("Ada");
        result.Active.ShouldBeTrue();
    }

    [Fact]
    public void Deserialize_NestedObject_ReturnsCorrectObject()
    {
        var toon = "Id: 1\nAddress:\n  City: Boulder\n  Street: Main St";
        var result = ToonSerializer.Deserialize<NestedObject>(toon);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Address.ShouldNotBeNull();
        result.Address.City.ShouldBe("Boulder");
        result.Address.Street.ShouldBe("Main St");
    }

    [Fact]
    public void Deserialize_InlineArray_ReturnsCorrectArray()
    {
        var toon = "[3]: a,b,c";
        var result = ToonSerializer.Deserialize<string[]>(toon);
        result.ShouldNotBeNull();
        result.Length.ShouldBe(3);
        result.ShouldBe(["a", "b", "c"]);
    }

    [Fact]
    public void Deserialize_IntArray_ReturnsCorrectArray()
    {
        var toon = "[3]: 1,2,3";
        var result = ToonSerializer.Deserialize<int[]>(toon);
        result.ShouldNotBeNull();
        result.Length.ShouldBe(3);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Deserialize_TabularArray_ReturnsCorrectObjects()
    {
        var toon = "[2]{Sku,Qty,Price}:\n  A1,2,9.99\n  B2,1,14.5";
        var result = ToonSerializer.Deserialize<TabularItem[]>(toon);
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result[0].Sku.ShouldBe("A1");
        result[0].Qty.ShouldBe(2);
        result[0].Price.ShouldBe(9.99m);
        result[1].Sku.ShouldBe("B2");
        result[1].Qty.ShouldBe(1);
        result[1].Price.ShouldBe(14.5m);
    }

    [Fact]
    public void Deserialize_ObjectWithArray_ReturnsCorrectObject()
    {
        var toon = "Tags[3]: admin,ops,dev";
        var result = ToonSerializer.Deserialize<TagsObject>(toon);
        result.ShouldNotBeNull();
        result.Tags.ShouldNotBeNull();
        result.Tags.Length.ShouldBe(3);
        result.Tags.ShouldBe(["admin", "ops", "dev"]);
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
        result.ShouldNotBeNull();
        result.PersonName.ShouldBe("Ada");
        result.Age.ShouldBe(30);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_SimpleObject()
    {
        var original = new SimpleObject { Id = 42, Name = "Test", Active = true };
        var toon = ToonSerializer.Serialize(original);
        var result = ToonSerializer.Deserialize<SimpleObject>(toon);
        result?.Id.ShouldBe(original.Id);
        result?.Name.ShouldBe(original.Name);
        result?.Active.ShouldBe(original.Active);
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
        result?.Id.ShouldBe(original.Id);
        result?.Address?.City.ShouldBe(original.Address?.City);
        result?.Address?.Street.ShouldBe(original.Address?.Street);
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
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result[0].Sku.ShouldBe(original[0].Sku);
        result[0].Qty.ShouldBe(original[0].Qty);
        result[0].Price.ShouldBe(original[0].Price);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Serialize_EmptyObject_ReturnsEmptyString()
    {
        var result = ToonSerializer.Serialize(new { });
        result.ShouldBe("");
    }

    [Fact]
    public void Serialize_ObjectWithNullValues()
    {
        var obj = new { Name = (string?)null, Age = 25 };
        var result = ToonSerializer.Serialize(obj);
        result.ShouldContain("Name: null");
        result.ShouldContain("Age: 25");
    }

    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        var result = ToonSerializer.Deserialize<SimpleObject>("");
        result.ShouldBeNull();
    }

    [Fact]
    public void Serialize_QuotedKeys()
    {
        var obj = new QuotedKeyObject { OrderId = 1, FullName = "Ada" };
        var result = ToonSerializer.Serialize(obj);
        result.ShouldContain("\"order:id\": 1");
        result.ShouldContain("\"full name\": Ada");
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
        result.ShouldNotBeNull();
        result.OrderId.ShouldBe(1);
        result.FullName.ShouldBe("Ada");
    }

    [Fact]
    public void Serialize_EscapedStrings()
    {
        var obj = new { Path = "C:\\Users\\test", Text = "line1\nline2" };
        var result = ToonSerializer.Serialize(obj);
        result.ShouldContain("\"C:\\\\Users\\\\test\"");
        result.ShouldContain("\"line1\\nline2\"");
    }

    [Fact]
    public void Deserialize_EscapedStrings()
    {
        var toon = "Path: \"C:\\\\Users\\\\test\"\nText: \"line1\\nline2\"";
        var result = ToonSerializer.Deserialize<PathObject>(toon);
        result.ShouldNotBeNull();
        result.Path.ShouldBe("C:\\Users\\test");
        result.Text.ShouldBe("line1\nline2");
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

        var expected = """
            Task: Our favorite hikes together
            Location: Boulder
            Friends[3]: ana,luis,sam
            Hikes[3]{Id,Name,DistanceKm,ElevationGain,Companion,WasSunny}:
              1,Blue Lake Trail,7.5,320,ana,true
              2,Ridge Overlook,9.2,540,luis,false
              3,Wildflower Loop,5.1,180,sam,true
            """;
        result.ShouldBe(expected);
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

        result.ShouldNotBeNull();
        result.Task.ShouldBe(original.Task);
        result.Location.ShouldBe(original.Location);
        result.Friends.ShouldBe(original.Friends);
        result.Hikes.Length.ShouldBe(original.Hikes.Length);
        result.Hikes[0].Name.ShouldBe(original.Hikes[0].Name);
        result.Hikes[0].DistanceKm.ShouldBe(original.Hikes[0].DistanceKm);
        result.Hikes[0].WasSunny.ShouldBe(original.Hikes[0].WasSunny);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Serialize_WithInvalidIndentSize_ThrowsArgumentOutOfRangeException(int indentSize)
    {
        var obj = new { Name = "Test" };
        Should.Throw<ArgumentOutOfRangeException>(() => ToonSerializer.Serialize(obj, indentSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SerializeGeneric_WithInvalidIndentSize_ThrowsArgumentOutOfRangeException(int indentSize)
    {
        var obj = new SimpleObject { Id = 1, Name = "Test", Active = true };
        Should.Throw<ArgumentOutOfRangeException>(() => ToonSerializer.Serialize<SimpleObject>(obj, indentSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Deserialize_WithInvalidIndentSize_ThrowsArgumentOutOfRangeException(int indentSize)
    {
        var toon = "Name: Test";
        Should.Throw<ArgumentOutOfRangeException>(() => ToonSerializer.Deserialize<SimpleObject>(toon, indentSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void DeserializeWithType_WithInvalidIndentSize_ThrowsArgumentOutOfRangeException(int indentSize)
    {
        var toon = "Name: Test";
        Should.Throw<ArgumentOutOfRangeException>(() => ToonSerializer.Deserialize(toon, typeof(SimpleObject), indentSize));
    }

    #endregion

    #region Field List Format Tests

    [Fact]
    public void Serialize_TabularArray_NoNewlineBetweenBraces()
    {
        var items = new[]
        {
            new TabularItem { Sku = "A1", Qty = 2, Price = 9.99m },
            new TabularItem { Sku = "B2", Qty = 1, Price = 14.5m }
        };
        var result = ToonSerializer.Serialize(items);
        
        // Ensure field list is on single line with no newlines between { and }
        result.ShouldContain("{Sku,Qty,Price}:");
        result.ShouldNotContain("{\n");
        result.ShouldNotContain("\n}");
    }

    [Fact]
    public void Serialize_ObjectWithTabularArray_NoNewlineBetweenBraces()
    {
        var obj = new
        {
            Items = new[]
            {
                new TabularItem { Sku = "A1", Qty = 2, Price = 9.99m },
                new TabularItem { Sku = "B2", Qty = 1, Price = 14.5m }
            }
        };
        var result = ToonSerializer.Serialize(obj);
        
        // Ensure field list is on single line with no newlines between { and }
        result.ShouldContain("{Sku,Qty,Price}:");
        result.ShouldNotContain("{\n");
        result.ShouldNotContain("\n}");
    }

    [Fact]
    public void Serialize_TabularArrayWithManyFields_NoNewlineBetweenBraces()
    {
        var hikes = new[]
        {
            new HikeInfo { Id = 1, Name = "Blue Lake Trail", DistanceKm = 7.5, ElevationGain = 320, Companion = "ana", WasSunny = true },
            new HikeInfo { Id = 2, Name = "Ridge Overlook", DistanceKm = 9.2, ElevationGain = 540, Companion = "luis", WasSunny = false }
        };
        var result = ToonSerializer.Serialize(hikes);
        
        // Ensure field list with many fields is still on single line
        result.ShouldContain("{Id,Name,DistanceKm,ElevationGain,Companion,WasSunny}:");
        result.ShouldNotContain("{\n");
        result.ShouldNotContain("\n}");
    }

    #endregion

    #region SPEC Compliance Tests

    // Note: The following tests are based on TOON SPEC requirements.
    // Many require strict-mode validation not yet implemented in this naive version.
    // Tests that fail due to missing strict-mode features are marked with comments.

    [Fact]
    public void Deserialize_ArrayCountMismatch_Inline_Throws()
    {
        var toon = "[2]: a"; // declared 2, provided 1
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<string[]>(toon));
    }

    [Fact]
    public void Deserialize_ArrayCountMismatch_ListItems_Throws()
    {
        var toon = "[2]:\n  - 1"; // declared 2, only 1 item
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<int[]>(toon));
    }

    [Fact]
    public void Deserialize_TabularRowWidthMismatch_Throws()
    {
        var toon = "[2]{id,name}:\n  1\n  2,Bob"; // first row has 1 value, should be 2
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<TabularItem[]>(toon));
    }

    [Fact]
    public void Deserialize_MissingColon_Throws()
    {
        var toon = "key value"; // missing colon
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<object>(toon));
    }

    [Fact]
    public void Deserialize_InvalidEscapeSequence_Throws()
    {
        // Debug: what does the test string look like?
        var toon = "Path: \"bad\\x\"";
        var colonIndex = toon.IndexOf(':');
        var valuepart = toon.Substring(colonIndex + 1).Trim();
        // valuepart should be: "bad\x" (with quotes)
        // After removing quotes: bad\x
        // UnescapeString should throw on \x
        
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<PathObject>(toon));
    }

    [Fact]
    public void Deserialize_UnterminatedString_Throws()
    {
        var toon = "Path: \"unterminated";
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<PathObject>(toon));
    }

    [Fact]
    public void Deserialize_IndentationNotMultipleOfIndentSize_Throws()
    {
        // "A:" at depth 0, next line has 1 leading space (indentSize default 2)
        var toon = "A:\n a: 1";
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<object>(toon));
    }

    [Fact]
    public void Deserialize_TabCharacterUsedForIndentation_Throws()
    {
        var toon = "A:\n\tB: 1"; // tab at beginning of line
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<object>(toon));
    }

    [Fact]
    public void Deserialize_BlankLineInsideArrayRows_Throws()
    {
        var toon = "[2]:\n  a\n\n  b"; // blank line between rows
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<string[]>(toon));
    }

    [Fact]
    public void Serialize_NoTrailingSpacesOrNewline()
    {
        var obj = new SimpleObject { Id = 1, Name = "A", Active = true };
        var result = ToonSerializer.Serialize(obj);
        // No trailing newline
        result.EndsWith("\n").ShouldBeFalse();
        // No trailing space at end of any line
        foreach (var line in result.Split('\n'))
        {
            line.EndsWith(" ").ShouldBeFalse();
        }
    }

    [Fact]
    public void Serialize_Number_TrailingZerosNormalized()
    {
        double value = 1.5000; // numeric literal equals 1.5, but test intent is canonical formatting
        var s = ToonSerializer.Serialize(value);
        s.ShouldBe("1.5");
    }

    [Fact]
    public void Serialize_Number_NoExponentNotation()
    {
        double value = 1e6; // 1000000
        var s = ToonSerializer.Serialize(value);
        s.ShouldBe("1000000");
    }

    [Fact]
    public void Deserialize_QuotedNumericInArray_RemainsString()
    {
        var toon = "[1]: \"42\"";
        var result = ToonSerializer.Deserialize<string[]>(toon);
        result.ShouldNotBeNull();
        result.Length.ShouldBe(1);
        result[0].ShouldBe("42"); // a string, not numeric
    }

    [Fact]
    public void Delimiter_PipeHeader_ActiveDelimiterIsPipe()
    {
        var toon = "[2|]: a|b";
        var result = ToonSerializer.Deserialize<string[]>(toon);
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result[0].ShouldBe("a");
        result[1].ShouldBe("b");
    }

    [Fact]
    public void Delimiter_TabHeader_ActiveDelimiterIsTab()
    {
        var toon = "[2\t]: x\ty";
        var arr = ToonSerializer.Deserialize<string[]>(toon);
        arr.ShouldBe(new[] { "x", "y" });
    }

    [Fact(Skip = "Requires nested array support")]
    public void Deserialize_ArraysOfArrays_ExpandedList_Parse()
    {
        var toon = "[2]:\n  - [2]: 1,2\n  - [2]: 3,4";
        var result = ToonSerializer.Deserialize<int[][]>(toon);
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result[0].ShouldBe(new[] { 1, 2 });
        result[1].ShouldBe(new[] { 3, 4 });
    }

    [Fact(Skip = "Requires list-item objects with tabular support")]
    public void Deserialize_ObjectsAsListItems_TabularFirstField()
    {
        var toon =
@"items[1]:
  - users[2]{id,name}:
      1,Ada
      2,Bob
    status: active";
        var root = ToonSerializer.Deserialize<ItemsContainer>(toon);
        root.ShouldNotBeNull();
        root.Items.ShouldNotBeNull();
        root.Items.Length.ShouldBe(1);
        var item = root.Items[0];
        item.Users.Length.ShouldBe(2);
        item.Users[0].Sku.ShouldBe("1");
        item.Status.ShouldBe("active");
    }

    [Fact]
    public void Deserialize_HeaderDelimiterFieldMismatch_Throws()
    {
        // Declares pipe but uses comma in the fields segment
        var toon = "[1|]{a,b}:\n  x|y";
        Should.Throw<FormatException>(() => ToonSerializer.Deserialize<object[]>(toon));
    }

    [Fact(Skip = "Requires tabular row vs key disambiguation")]
    public void Deserialize_TabularRowVsKeyDisambiguation_EndRowsOnKey()
    {
        var toon =
@"data[2]{id,name}:
  1,Alice
  note: Something else
key: value";
        var doc = ToonSerializer.Deserialize<RootWithData>(toon);
        doc.ShouldNotBeNull();
        doc.Data.Length.ShouldBe(1);
        doc.Key.ShouldBe("value");
    }

    [Fact(Skip = "Requires expandPaths configuration")]
    public void Deserialize_DottedKeyTreatedLiteral_ByDefault()
    {
        var toon = "user.name: Ada";
        // Use a target type with a property literally named "user.name" via ToonPropertyName
        var result = ToonSerializer.Deserialize<UserDotNameHolder>(toon);
        result.ShouldNotBeNull();
        result.UserDotName.ShouldBe("Ada");
    }

    // Helper types for SPEC compliance tests
    public record ItemsContainer
    {
        public ListWrapper[] Items { get; init; } = [];
    }

    public record ListWrapper
    {
        public TabularItem[] Users { get; init; } = [];
        public string Status { get; init; } = "";
    }

    public record RootWithData
    {
        public TabularItem[] Data { get; init; } = [];
        public string Key { get; init; } = "";
    }

    public record UserDotNameHolder
    {
        [ToonPropertyName("user.name")]
        public string UserDotName { get; init; } = "";
    }

    #endregion
}
