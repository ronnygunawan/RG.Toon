using Shouldly;

namespace RG.Toon.Tests;

/// <summary>
/// Tests for source-generated TOON serializers
/// </summary>
public class SourceGeneratedSerializerTests
{
    #region Serialization Tests

    [Fact]
    public void SourceGenerated_Serialize_SimplePerson_ProducesSameOutputAsReflection()
    {
        var person = new SimplePerson { Id = 1, Name = "Alice", Age = 30 };

        var reflectionResult = ToonSerializer.Serialize(person);
        var sourceGenResult = SimplePersonToonSerializer.Serialize(person);

        sourceGenResult.ShouldBe(reflectionResult);
    }

    [Fact]
    public void SourceGenerated_Serialize_SimplePerson_ProducesCorrectOutput()
    {
        var person = new SimplePerson { Id = 42, Name = "Bob", Age = 25 };

        var result = SimplePersonToonSerializer.Serialize(person);

        result.ShouldBe("Id: 42\nName: Bob\nAge: 25");
    }

    [Fact]
    public void SourceGenerated_Serialize_NullObject_ReturnsNull()
    {
        SimplePerson? person = null;

        var result = SimplePersonToonSerializer.Serialize(person!);

        result.ShouldBe("null");
    }

    [Fact]
    public void SourceGenerated_Serialize_WithNullableProperties_HandlesNulls()
    {
        var person = new PersonWithNullableProperties
        {
            NullableInt = null,
            NullableString = "test",
            NullableBool = true
        };

        var reflectionResult = ToonSerializer.Serialize(person);
        var sourceGenResult = PersonWithNullablePropertiesToonSerializer.Serialize(person);

        sourceGenResult.ShouldBe(reflectionResult);
    }

    [Fact]
    public void SourceGenerated_Serialize_WithVariousTypes_HandlesAllTypes()
    {
        var person = new PersonWithVariousTypes
        {
            Id = 1,
            Name = "Charlie",
            Score = 95.5,
            IsActive = true,
            Balance = 1234.56m
        };

        var reflectionResult = ToonSerializer.Serialize(person);
        var sourceGenResult = PersonWithVariousTypesToonSerializer.Serialize(person);

        sourceGenResult.ShouldBe(reflectionResult);
    }

    [Fact]
    public void SourceGenerated_Serialize_WithAttributes_RespectsAttributes()
    {
        var person = new PersonWithAttributes
        {
            Id = 1,
            Name = "Diana",
            InternalId = "SECRET123",
            Age = 28
        };

        var sourceGenResult = PersonWithAttributesToonSerializer.Serialize(person);

        // Should use custom property name and ignore InternalId
        sourceGenResult.ShouldContain("full_name: Diana");
        sourceGenResult.ShouldNotContain("InternalId");
        sourceGenResult.ShouldNotContain("SECRET123");
    }

    [Fact]
    public void SourceGenerated_Serialize_WithSpecialCharactersInName_QuotesCorrectly()
    {
        var person = new SimplePerson { Id = 1, Name = "O'Brien", Age = 30 };

        var reflectionResult = ToonSerializer.Serialize(person);
        var sourceGenResult = SimplePersonToonSerializer.Serialize(person);

        sourceGenResult.ShouldBe(reflectionResult);
    }

    [Fact]
    public void SourceGenerated_Serialize_WithEmptyString_QuotesCorrectly()
    {
        var person = new SimplePerson { Id = 1, Name = "", Age = 30 };

        var reflectionResult = ToonSerializer.Serialize(person);
        var sourceGenResult = SimplePersonToonSerializer.Serialize(person);

        sourceGenResult.ShouldBe(reflectionResult);
    }

    #endregion

    #region Deserialization Tests

    [Fact]
    public void SourceGenerated_Deserialize_SimplePerson_ReturnsCorrectObject()
    {
        var toon = "Id: 42\nName: Alice\nAge: 30";

        var result = SimplePersonToonSerializer.Deserialize(toon);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(42);
        result.Name.ShouldBe("Alice");
        result.Age.ShouldBe(30);
    }

    [Fact]
    public void SourceGenerated_Deserialize_SimplePerson_ProducesSameResultAsReflection()
    {
        var toon = "Id: 1\nName: Bob\nAge: 25";

        var reflectionResult = ToonSerializer.Deserialize<SimplePerson>(toon);
        var sourceGenResult = SimplePersonToonSerializer.Deserialize(toon);

        sourceGenResult.ShouldNotBeNull();
        reflectionResult.ShouldNotBeNull();
        sourceGenResult.Id.ShouldBe(reflectionResult.Id);
        sourceGenResult.Name.ShouldBe(reflectionResult.Name);
        sourceGenResult.Age.ShouldBe(reflectionResult.Age);
    }

    [Fact]
    public void SourceGenerated_Deserialize_EmptyString_ReturnsNull()
    {
        var result = SimplePersonToonSerializer.Deserialize("");

        result.ShouldBeNull();
    }

    [Fact]
    public void SourceGenerated_Deserialize_NullString_ReturnsNull()
    {
        var result = SimplePersonToonSerializer.Deserialize(null!);

        result.ShouldBeNull();
    }

    [Fact]
    public void SourceGenerated_Deserialize_WithNullableProperties_HandlesNulls()
    {
        var toon = "NullableInt: null\nNullableString: test\nNullableBool: true";

        var result = PersonWithNullablePropertiesToonSerializer.Deserialize(toon);

        result.ShouldNotBeNull();
        result.NullableInt.ShouldBeNull();
        result.NullableString.ShouldBe("test");
        result.NullableBool.ShouldBe(true);
    }

    [Fact]
    public void SourceGenerated_Deserialize_WithVariousTypes_HandlesAllTypes()
    {
        var toon = "Id: 1\nName: Charlie\nScore: 95.5\nIsActive: true\nBalance: 1234.56";

        var result = PersonWithVariousTypesToonSerializer.Deserialize(toon);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Charlie");
        result.Score.ShouldBe(95.5);
        result.IsActive.ShouldBe(true);
        result.Balance.ShouldBe(1234.56m);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void SourceGenerated_RoundTrip_SimplePerson_PreservesData()
    {
        var original = new SimplePerson { Id = 99, Name = "Eve", Age = 35 };

        var serialized = SimplePersonToonSerializer.Serialize(original);
        var deserialized = SimplePersonToonSerializer.Deserialize(serialized);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(original.Id);
        deserialized.Name.ShouldBe(original.Name);
        deserialized.Age.ShouldBe(original.Age);
    }

    [Fact]
    public void SourceGenerated_RoundTrip_WithVariousTypes_PreservesAllData()
    {
        var original = new PersonWithVariousTypes
        {
            Id = 42,
            Name = "Frank",
            Score = 88.9,
            IsActive = false,
            Balance = 9999.99m
        };

        var serialized = PersonWithVariousTypesToonSerializer.Serialize(original);
        var deserialized = PersonWithVariousTypesToonSerializer.Deserialize(serialized);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(original.Id);
        deserialized.Name.ShouldBe(original.Name);
        deserialized.Score.ShouldBe(original.Score);
        deserialized.IsActive.ShouldBe(original.IsActive);
        deserialized.Balance.ShouldBe(original.Balance);
    }

    [Fact]
    public void SourceGenerated_RoundTrip_MatchesReflectionBased()
    {
        var original = new SimplePerson { Id = 123, Name = "Grace", Age = 45 };

        // Serialize with source-generated, deserialize with reflection
        var sourceGenSerialized = SimplePersonToonSerializer.Serialize(original);
        var reflectionDeserialized = ToonSerializer.Deserialize<SimplePerson>(sourceGenSerialized);

        reflectionDeserialized.ShouldNotBeNull();
        reflectionDeserialized.Id.ShouldBe(original.Id);
        reflectionDeserialized.Name.ShouldBe(original.Name);
        reflectionDeserialized.Age.ShouldBe(original.Age);

        // Serialize with reflection, deserialize with source-generated
        var reflectionSerialized = ToonSerializer.Serialize(original);
        var sourceGenDeserialized = SimplePersonToonSerializer.Deserialize(reflectionSerialized);

        sourceGenDeserialized.ShouldNotBeNull();
        sourceGenDeserialized.Id.ShouldBe(original.Id);
        sourceGenDeserialized.Name.ShouldBe(original.Name);
        sourceGenDeserialized.Age.ShouldBe(original.Age);
    }

    #endregion
}
