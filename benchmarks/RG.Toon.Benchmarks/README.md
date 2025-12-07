# RG.Toon Benchmarks

This project contains benchmarks comparing the performance of different TOON serialization implementations.

## Running the Benchmarks

To run the benchmarks, execute:

```bash
dotnet run -c Release
```

## Benchmark Categories

### Serialization Benchmarks
- **Serialize_Reflection**: Baseline reflection-based serialization of arrays
- **Serialize_SingleObject_Reflection**: Reflection-based serialization of single objects

### Deserialization Benchmarks
- **Deserialize_Reflection**: Baseline reflection-based deserialization of arrays
- **Deserialize_SingleObject_Reflection**: Reflection-based deserialization of single objects

## Metrics

Each benchmark measures:
- **Mean execution time**: Average time taken per operation
- **Memory allocation**: Total memory allocated during the operation
- **Standard deviation**: Variation in execution time

## Test Data

The benchmarks use a simple `Person` class with the following properties:
- Id (int)
- Name (string)
- Age (int)
- Email (string)
- Active (bool)

Tests are run on both single objects and arrays of 5 objects.
