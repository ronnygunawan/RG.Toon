# TOON Serializer Benchmarks

Benchmark results comparing reflection-based and source-generated TOON serialization performance.

## Environment

```
BenchmarkDotNet v0.15.6, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
```

## Results

| Method                                   | Mean        | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------------- |------------:|---------:|---------:|------:|-------:|----------:|------------:|
| Serialize_Reflection                     | 21,354.9 ns | 92.18 ns | 81.72 ns |  1.00 | 0.6104 |   11514 B |        1.00 |
| Deserialize_Reflection                   | 12,747.1 ns | 58.61 ns | 48.94 ns |  0.60 | 0.5493 |    9434 B |        0.82 |
| Serialize_SingleObject_Reflection        |  1,761.5 ns | 11.33 ns | 10.60 ns |  0.08 | 0.0477 |     824 B |        0.07 |
| Deserialize_SingleObject_Reflection      |  2,436.6 ns | 11.90 ns |  9.29 ns |  0.11 | 0.1068 |    1792 B |        0.16 |
| Serialize_SingleObject_SourceGenerated   |    500.2 ns |  2.23 ns |  1.98 ns |  0.02 | 0.0811 |    1368 B |        0.12 |
| Deserialize_SingleObject_SourceGenerated |  2,389.2 ns |  5.83 ns |  4.87 ns |  0.11 | 0.1068 |    1792 B |        0.16 |

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

### Serialization Performance Comparison

#### Array Serialization
- **Reflection-based**: 21.35 μs (baseline)
- Currently no source-generated array serialization (arrays use reflection)

#### Single Object Serialization
- **Reflection-based**: 1.76 μs
- **Source-generated**: 500.2 ns
- **Performance gain**: **3.5x faster** (Ratio: 0.02 vs 0.08)
- **Time saved**: 1.26 μs per operation (71.6% reduction)

### Deserialization Performance

#### Array Deserialization
- **Reflection-based**: 12.75 μs
- Currently no source-generated array deserialization (arrays use reflection)

#### Single Object Deserialization
- **Reflection-based**: 2.44 μs
- **Source-generated**: 2.39 μs
- **Performance gain**: **~2% faster** (marginal, as deserialization uses reflection internally)

### Memory Allocation Comparison

#### Single Object Serialization
- **Reflection-based**: 824 bytes
- **Source-generated**: 1,368 bytes
- **Trade-off**: Source-generated uses 66% more memory but is 3.5x faster

#### Single Object Deserialization
- **Reflection-based**: 1,792 bytes
- **Source-generated**: 1,792 bytes (identical, both use reflection-based deserializer)

### GC Pressure
- Low GC pressure across all benchmarks
- Gen0 collections range from 0.0477 to 0.6104 per 1000 operations
- Source-generated serialization has slightly higher Gen0 (0.0811 vs 0.0477)

## Performance Summary

### ✨ Source-Generated Advantages

1. **Serialization Speed**: 3.5x faster for single objects
   - Reflection: 1,761.5 ns
   - Source-generated: 500.2 ns
   
2. **Consistent Performance**: Lower standard deviation in source-generated (1.98 ns vs 10.60 ns)

3. **Scalability**: Direct property access eliminates reflection overhead

### ⚖️ Trade-offs

1. **Memory**: Source-generated uses 66% more memory for serialization (1,368 vs 824 bytes)
2. **Deserialization**: Currently delegates to reflection, minimal performance difference
3. **Array Support**: Arrays currently use reflection-based serialization

## Conclusions

1. **Source-generated serialization provides significant performance gains** (3.5x faster)
2. **Best for single object serialization** where speed is critical
3. **Memory trade-off is acceptable** for most use cases given the speed improvement
4. **Deserialization optimization** could be a future enhancement
5. **Array serialization optimization** could be added in future versions

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
