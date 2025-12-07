# Source-Generated TOON Serializer Implementation

This document summarizes the implementation of the source-generated version of RG.Toon.

## Overview

This implementation adds compile-time code generation to RG.Toon, providing optimized serialization performance while maintaining the existing reflection-based API as the default.

## Architecture

### 1. Source Generator (`RG.Toon.SourceGenerator`)

A Roslyn source generator that produces optimized serialization code at compile-time:

- **ToonSourceGenerator.cs**: Main source generator implementation
  - Detects types marked with `[ToonSerializable]` attribute
  - Generates `{TypeName}ToonSerializer` static classes
  - Uses static compiled Regex for efficient property name validation
  
- **Generated Code**: 
  - `Serialize` method: Direct property access without reflection
  - `Deserialize` method: Currently delegates to reflection-based implementation

### 2. Benchmarks Project (`RG.Toon.Benchmarks`)

Performance testing suite using BenchmarkDotNet:

- **Serialization benchmarks**: Arrays and single objects
- **Deserialization benchmarks**: Arrays and single objects
- **Metrics**: Execution time and memory allocation
- **Test data**: Person class with 5 properties

### 3. Integration

- Added to main solution file
- Source generator referenced as analyzer in benchmark project
- Minimal changes to existing codebase
- All 111 existing tests pass

## Usage

### Using Source Generation

```csharp
// Mark your type with the attribute
[ToonSerializable]
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// Use the generated serializer
var person = new Person { Id = 1, Name = "Alice", Age = 30 };
var toon = PersonToonSerializer.Serialize(person);
var deserialized = PersonToonSerializer.Deserialize(toon);
```

### Running Benchmarks

```bash
cd benchmarks/RG.Toon.Benchmarks
dotnet run -c Release
```

## Design Decisions

1. **Minimal Implementation**: The source generator produces simple, readable code that delegates actual value serialization to the existing `ToonSerializer` class
   
2. **Opt-in**: Source generation requires explicit `[ToonSerializable]` attribute, keeping the reflection-based API as the default

3. **Compatibility**: Generated code produces identical output to reflection-based serialization

4. **Performance Focus**: Source generator uses static compiled Regex and generates direct property access code

## Benefits

- **Performance**: Eliminates reflection overhead for property access
- **AOT Compatibility**: Generated code works with ahead-of-time compilation
- **Type Safety**: Compile-time code generation catches issues early
- **Measurable**: Included benchmarks allow quantifying performance improvements

## Testing

- All 111 existing tests pass
- Created separate test project to verify source generator functionality
- CodeQL security scan: 0 alerts
- Code review: All feedback addressed

## Future Enhancements

Potential improvements for future iterations:

1. Generate optimized deserialization code (currently uses reflection)
2. Support for nested objects and collections in generated code
3. Incremental source generation optimizations
4. Additional benchmark scenarios

## References

- Issue: ronnygunawan/RG.Toon#1
- Reference implementation: https://github.com/ronnygunawan/csv-serializer
- TOON Specification: https://github.com/toon-format/spec
