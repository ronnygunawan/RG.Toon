# RG.Toon

[![CI](https://github.com/ronnygunawan/RG.Toon/actions/workflows/ci.yml/badge.svg)](https://github.com/ronnygunawan/RG.Toon/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/RG.Toon.svg)](https://www.nuget.org/packages/RG.Toon/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

A .NET implementation of the [TOON (Token-Oriented Object Notation)](https://github.com/toon-format/spec) serializer/deserializer.

TOON is a compact, human-readable encoding of the JSON data model, particularly efficient for arrays of uniform objects. It's designed to minimize LLM token consumptions while maintaining clear structure.

## Installation

```bash
dotnet add package RG.Toon
```

## Quick Start

```csharp
using RG.Toon;

// Serialize an object to TOON
var items = new[]
{
    new { Id = 1, Name = "Alice", Active = true },
    new { Id = 2, Name = "Bob", Active = false }
};
string toon = ToonSerializer.Serialize(items);
// Output:
// [2]{Id,Name,Active}:
//   1,Alice,true
//   2,Bob,false

// Deserialize TOON back to objects
var result = ToonSerializer.Deserialize<Item[]>(toon);
```

## Features

- **Compact Format**: TOON uses tabular format for arrays of uniform objects, reducing redundancy
- **Human-Readable**: Clean indentation-based syntax similar to YAML
- **Full JSON Data Model Support**: Objects, arrays, strings, numbers, booleans, and null
- **Custom Property Naming**: Use `[ToonPropertyName]` to customize serialized property names
- **Property Ignoring**: Use `[ToonIgnore]` to exclude properties from serialization

## Format Examples

### Simple Objects

```csharp
var obj = new { Id = 123, Name = "Ada", Active = true };
var toon = ToonSerializer.Serialize(obj);
```

Output:
```
Id: 123
Name: Ada
Active: true
```

### Nested Objects

```csharp
var obj = new
{
    User = new
    {
        Name = "Ada",
        Address = new { City = "Boulder", Street = "Main St" }
    }
};
var toon = ToonSerializer.Serialize(obj);
```

Output:
```
User:
  Name: Ada
  Address:
    City: Boulder
    Street: Main St
```

### Primitive Arrays

```csharp
var obj = new { Tags = new[] { "admin", "ops", "dev" } };
var toon = ToonSerializer.Serialize(obj);
```

Output:
```
Tags[3]: admin,ops,dev
```

### Tabular Arrays (Arrays of Uniform Objects)

```csharp
var items = new[]
{
    new { Sku = "A1", Qty = 2, Price = 9.99m },
    new { Sku = "B2", Qty = 1, Price = 14.5m }
};
var obj = new { Items = items };
var toon = ToonSerializer.Serialize(obj);
```

Output:
```
Items[2]{Sku,Qty,Price}:
  A1,2,9.99
  B2,1,14.5
```

## Attributes

### ToonPropertyName

Use `[ToonPropertyName]` to customize the name of a property in the serialized output:

```csharp
public record Person
{
    [ToonPropertyName("name")]
    public required string PersonName { get; init; }
    
    public int Age { get; init; }
}

var person = new Person { PersonName = "Ada", Age = 30 };
var toon = ToonSerializer.Serialize(person);
// Output:
// name: Ada
// Age: 30
```

### ToonIgnore

Use `[ToonIgnore]` to exclude a property from serialization:

```csharp
public record User
{
    public required string Name { get; init; }
    
    [ToonIgnore]
    public string NormalizedName => Name.ToUpperInvariant();
    
    public int Age { get; init; }
}

var user = new User { Name = "Ada", Age = 30 };
var toon = ToonSerializer.Serialize(user);
// Output:
// Name: Ada
// Age: 30
// (NormalizedName is not included)
```

## API Reference

### ToonSerializer.Serialize

```csharp
// Serialize any object to TOON string
public static string Serialize<T>(T value, int indentSize = 2);
public static string Serialize(object? value, int indentSize = 2);
```

### ToonSerializer.Deserialize

```csharp
// Deserialize TOON string to strongly-typed object
public static T? Deserialize<T>(string toon, int indentSize = 2);
public static object? Deserialize(string toon, Type type, int indentSize = 2);
```

## Performance

RG.Toon includes both reflection-based and source-generated implementations:

### Reflection-Based (Default)

The library uses reflection by default for maximum flexibility and ease of use. This works with any type at runtime without requiring any additional setup.

### Source-Generated (Experimental)

For performance-critical scenarios, RG.Toon includes a source generator that can produce optimized serialization code at compile time. To use source generation:

1. Add the `ToonSerializable` attribute to your types:

```csharp
[ToonSerializable]
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}
```

2. Use the generated serializer:

```csharp
// Generated code provides PersonToonSerializer class
var toon = PersonToonSerializer.Serialize(person);
var deserialized = PersonToonSerializer.Deserialize(toon);
```

### Benchmarks

A benchmark project is included to compare reflection-based vs source-generated performance. To run benchmarks:

```bash
cd benchmarks/RG.Toon.Benchmarks
dotnet run -c Release
```

## Quoting Rules

TOON follows specific quoting rules to maintain unambiguous parsing:

- **Quoted Strings**: Empty strings, strings that look like booleans (`true`, `false`), null, numbers, or contain special characters (`:`, `,`, `"`, `\`, `[`, `]`, `{`, `}`, newlines)
- **Unquoted Strings**: Safe strings containing alphanumeric characters, underscores, dots, Unicode, and emoji

## Escape Sequences

The following escape sequences are supported within quoted strings:

| Escape | Character |
|--------|-----------|
| `\\` | Backslash |
| `\"` | Double quote |
| `\n` | Newline |
| `\r` | Carriage return |
| `\t` | Tab |

## TOON Specification

This library implements [TOON Specification v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md).

For detailed format specification, encoding rules, and conformance requirements, refer to the official specification.

## License

[MIT](./LICENSE) License © Ronny Gunawan
