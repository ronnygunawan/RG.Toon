# Copilot Instructions for RG.Toon Repository

This document provides guidance for maintaining and extending the RG.Toon TOON serializer library.

## Project Overview

RG.Toon is a .NET library that implements the TOON (Token-Oriented Object Notation) v3.0 format specification. TOON is a compact, human-readable encoding of the JSON data model, particularly efficient for arrays of uniform objects and designed to minimize LLM token consumption.

**Specification:** https://github.com/toon-format/spec

## Code Style and Standards

### String Literals

- **Use raw string literals (triple quotes)** for multiline strings in tests and examples
- Example:
  ```csharp
  var toon = """
      Id: 123
      Name: Ada
      Active: true
      """;
  ```
- For strings with quotes, use raw string literals to avoid escaping:
  ```csharp
  var toon = """Path: "C:\Users\test" """;
  ```

### Testing

- **Framework:** xUnit with Shouldly assertions
- **Test naming:** Use descriptive names like `Deserialize_SimpleObject_ReturnsCorrectObject`
- **Test organization:** Group related tests together, use Theory for parameterized tests
- All tests must pass (100% pass rate expected)

### Code Organization

- **Library:** `src/RG.Toon/`
  - `ToonSerializer.cs` - Main serialization/deserialization logic
  - `ToonPropertyNameAttribute.cs` - Attribute for custom property names
  - `ToonIgnoreAttribute.cs` - Attribute to exclude properties from serialization
- **Tests:** `tests/RG.Toon.Tests/`
  - `ToonSerializerTests.cs` - Comprehensive test suite

## Implementation Approach

This is a **"naive version"** implementation, meaning:
- ✅ Straightforward, readable code
- ✅ Focus on correctness and SPEC compliance
- ❌ No source generators or advanced optimizations
- ❌ No runtime IL generation or reflection.emit

## SPEC Compliance Features

### Core Serialization
- Primitives: numbers, strings, booleans, null
- Objects: key-value pairs with `:` separator
- Arrays: inline for primitives `[N]: a,b,c`, tabular for objects
- Nested structures with indentation
- Proper quoting for reserved literals, delimiters, control characters
- Escape sequences: `\\`, `\"`, `\n`, `\r`, `\t`

### Strict-Mode Validation
- Array count mismatch detection (inline and list items)
- Tabular row width consistency
- Missing colon detection in key-value pairs
- Indentation validation (must be multiples of indentSize)
- Tab character prohibition in indentation
- Blank line detection in arrays

### Custom Delimiters
- Support for pipe `|` and tab `\t` delimiters
- Quote-aware delimiter detection
- Delimiter consistency validation

### Advanced Features
- Nested array parsing
- Dotted key literal handling (keys containing `.`)
- Tabular row vs key disambiguation
- List-item objects with tabular first field
- Case-insensitive property name matching

## Making Changes

### When Adding Features
1. Check the TOON spec first
2. Add failing tests that demonstrate the expected behavior
3. Implement the feature with minimal code changes
4. Ensure all tests pass (100% pass rate)
5. Update README.md if public API changes

### When Fixing Bugs
1. Add a test that reproduces the bug
2. Fix the bug with minimal changes
3. Verify all tests still pass
4. Consider edge cases and add additional tests if needed

### Code Review Checklist
- [ ] All tests passing (100%)
- [ ] No floating-point equality checks (use bit comparison)
- [ ] Proper error messages with `FormatException` for invalid TOON
- [ ] Case-insensitive property matching maintained
- [ ] Indentation validation working correctly
- [ ] No security vulnerabilities (run CodeQL)

## Building and Testing

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## CI/CD Pipeline

- **Workflow:** `.github/workflows/ci.yml`
- **Triggers:** Push and PR to `master` branch
- **Steps:** Build → Test → Report
- **Target:** .NET 10

## Common Pitfalls to Avoid

1. **Don't use `==` for float/double zero checks** - Use `BitConverter.DoubleToInt64Bits` for proper +0/-0 handling
2. **Don't use `StringComparer.Ordinal`** - Use `OrdinalIgnoreCase` for property matching
3. **Don't forget indentation validation** - All indents must be multiples of `indentSize`
4. **Don't allow tabs in indentation** - Tabs are only valid as delimiters
5. **Don't skip array count validation** - Strict mode requires exact counts
6. **Always use raw string literals** - For multiline test data and examples

## Useful Patterns

### Parsing Pattern
```csharp
// Use ParseContext to track position and state
var context = new ParseContext { Lines = lines, Position = 0 };
while (context.Position < context.Lines.Length) {
    // Parse logic here
}
```

### Validation Pattern
```csharp
// Validate early, fail fast with clear messages
if (condition) {
    throw new FormatException("Clear description of what went wrong");
}
```

### Testing Pattern
```csharp
[Fact]
public void FeatureName_Scenario_ExpectedBehavior()
{
    // Arrange
    var toon = """
        Key: Value
        """;
    
    // Act
    var result = ToonSerializer.Deserialize<Type>(toon);
    
    // Assert
    result.ShouldNotBeNull();
    result.Property.ShouldBe(expectedValue);
}
```

## Resources

- [TOON Specification](https://github.com/toon-format/spec)
- [xUnit Documentation](https://xunit.net/)
- [Shouldly Documentation](https://docs.shouldly.org/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)

## Future Enhancements (Out of Scope for Naive Version)

- Source generator for compile-time serialization
- Streaming API for large files
- Custom type converters
- Performance optimizations
- Memory pooling for reduced allocations
