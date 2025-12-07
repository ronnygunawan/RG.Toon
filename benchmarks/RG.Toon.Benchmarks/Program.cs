using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using RG.Toon;

namespace RG.Toon.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ToonSerializerBenchmarks>();
    }
}

[MemoryDiagnoser]
public class ToonSerializerBenchmarks
{
    private Person[] _testData = [];
    private string _serializedData = "";

    [GlobalSetup]
    public void Setup()
    {
        _testData = new[]
        {
            new Person { Id = 1, Name = "Alice", Age = 30, Email = "alice@example.com", Active = true },
            new Person { Id = 2, Name = "Bob", Age = 25, Email = "bob@example.com", Active = false },
            new Person { Id = 3, Name = "Charlie", Age = 35, Email = "charlie@example.com", Active = true },
            new Person { Id = 4, Name = "Diana", Age = 28, Email = "diana@example.com", Active = true },
            new Person { Id = 5, Name = "Eve", Age = 32, Email = "eve@example.com", Active = false }
        };

        _serializedData = ToonSerializer.Serialize(_testData);
    }

    [Benchmark(Baseline = true)]
    public string Serialize_Reflection()
    {
        return ToonSerializer.Serialize(_testData);
    }

    [Benchmark]
    public Person[]? Deserialize_Reflection()
    {
        return ToonSerializer.Deserialize<Person[]>(_serializedData);
    }

    // Benchmarks for single object
    [Benchmark]
    public string Serialize_SingleObject_Reflection()
    {
        return ToonSerializer.Serialize(_testData[0]);
    }

    [Benchmark]
    public Person? Deserialize_SingleObject_Reflection()
    {
        var serialized = ToonSerializer.Serialize(_testData[0]);
        return ToonSerializer.Deserialize<Person>(serialized);
    }
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
    public bool Active { get; set; }
}
