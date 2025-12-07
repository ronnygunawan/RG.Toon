# TOON Serializer Benchmarks

Benchmark results for RG.Toon serialization performance.

## Environment

```
BenchmarkDotNet v0.15.6, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
```

## Results

| Method                              | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------------ |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| Serialize_Reflection                | 21.520 μs | 0.2489 μs | 0.2207 μs |  1.00 | 0.6104 |   11514 B |        1.00 |
| Deserialize_Reflection              | 12.884 μs | 0.0587 μs | 0.0459 μs |  0.60 | 0.5493 |    9434 B |        0.82 |
| Serialize_SingleObject_Reflection   |  1.768 μs | 0.0097 μs | 0.0086 μs |  0.08 | 0.0477 |     824 B |        0.07 |
| Deserialize_SingleObject_Reflection |  4.367 μs | 0.0243 μs | 0.0215 μs |  0.20 | 0.1526 |    2617 B |        0.23 |

## Benchmark Details

### Test Data

The benchmarks use a `Person` class with the following properties:
- `Id` (int)
- `Name` (string)
- `Age` (int)
- `Email` (string)
- `Active` (bool)

Array benchmarks use 5 Person objects, single object benchmarks use 1 Person object.

### Metrics Explained

- **Mean**: Average execution time per operation
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Ratio**: Performance relative to baseline (Serialize_Reflection = 1.00)
- **Gen0**: GC Generation 0 collections per 1000 operations
- **Allocated**: Memory allocated per operation (managed only)
- **Alloc Ratio**: Memory allocation relative to baseline

## Key Findings

### Serialization Performance
- **Array Serialization**: 21.52 μs for 5 objects (baseline)
- **Single Object Serialization**: 1.77 μs (12.5x faster than array, 0.08x ratio)

### Deserialization Performance
- **Array Deserialization**: 12.88 μs for 5 objects (1.67x faster than serialization)
- **Single Object Deserialization**: 4.37 μs (2.47x slower than single serialization, 2.94x faster than array deserialization)

### Memory Allocation
- **Array Serialization**: 11,514 bytes allocated
- **Array Deserialization**: 9,434 bytes (18% less than serialization)
- **Single Object Serialization**: 824 bytes (7% of array allocation)
- **Single Object Deserialization**: 2,617 bytes (23% of array allocation)

### GC Pressure
- Low GC pressure across all benchmarks
- Gen0 collections range from 0.0477 to 0.6104 per 1000 operations
- Most memory allocations stay in Gen0, indicating short-lived objects

## Conclusions

1. **Deserialization is faster than serialization** for arrays (40% faster)
2. **Memory-efficient**: Single object operations use significantly less memory (7% for serialization, 23% for deserialization)
3. **Low GC overhead**: Minimal garbage collection pressure across all operations
4. **Consistent performance**: Low standard deviation indicates stable, predictable performance

## Running the Benchmarks

To reproduce these results:

```bash
cd benchmarks/RG.Toon.Benchmarks
dotnet run -c Release
```

The full results including detailed statistics and histograms are available in:
- `BenchmarkDotNet.Artifacts/results/RG.Toon.Benchmarks.ToonSerializerBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts/results/RG.Toon.Benchmarks.ToonSerializerBenchmarks-report.html`
- `BenchmarkDotNet.Artifacts/results/RG.Toon.Benchmarks.ToonSerializerBenchmarks-report-github.md`
