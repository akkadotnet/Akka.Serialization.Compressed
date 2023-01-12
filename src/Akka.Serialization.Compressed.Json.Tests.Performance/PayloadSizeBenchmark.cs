
using System;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Akka.Serialization.Compressed.Json.Tests.Performance;

[Config(typeof(MicroBenchmarkConfig))] 
public class PayloadSizeBenchmark
{
    private const string AlphaNumeric = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        
    [Params(1024, 1024 * 1024, 10 * 1024 * 1024)]
    public int PayloadSize { get; set; }

    private BigData? _data;
    private byte[]? _raw;
    private CompressedJsonSerializer? _serializer;

    [GlobalSetup]
    public void Setup()
    {
        _serializer = new CompressedJsonSerializer(null);
            
        var rnd = new Random();
        var sb = new StringBuilder();
        for (var i = 0; i < PayloadSize; ++i)
        {
            sb.Append(AlphaNumeric[rnd.Next(0, AlphaNumeric.Length)]);
        }

        _data = new BigData
        {
            Message = sb.ToString()
        };

        _raw = _serializer.ToBinary(_data);
    }

    [Benchmark]
    public byte[] Serialize()
    {
        return _serializer!.ToBinary(_data!);
    }
        
    [Benchmark]
    public BigData Deserialize()
    {
        return _serializer!.FromBinary<BigData>(_raw);
    }
}

public class BigData
{
    public string? Message { get; set; }
}
