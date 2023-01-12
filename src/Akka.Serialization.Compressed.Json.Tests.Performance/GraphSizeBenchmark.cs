// -----------------------------------------------------------------------
//  <copyright file="GraphSizeBenchmark.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Akka.Serialization.Compressed.Json.Tests.Performance;

[Config(typeof(MicroBenchmarkConfig))]
public class GraphSizeBenchmark
{
    [Params(3, 5)]
    public int Depth { get; set; }
    
    [Params(1, 3, 5)]
    public int ChildCount { get; set; }

    private NestedClass? _data;
    private byte[]? _raw;
    private CompressedJsonSerializer? _serializer;

    [GlobalSetup]
    public void Setup()
    {
        _serializer = new CompressedJsonSerializer(null);
            
        _data = new NestedClass();
        Populate(_data, Depth);
        
        _raw = _serializer.ToBinary(_data);
    }

    [Benchmark]
    public byte[] Serialize()
    {
        return _serializer!.ToBinary(_data!);
    }
        
    [Benchmark]
    public NestedClass Deserialize()
    {
        return _serializer!.FromBinary<NestedClass>(_raw);
    }
    
    private void Populate(NestedClass data, int depth)
    {
        if (depth < 0)
            return;
        for (var i = 0; i < ChildCount; ++i)
        {
            var node = new NestedClass();
            data.Children.Add(node);
            Populate(node, depth - 1);
        }
    }
}

public class NestedClass
{
    public string Id { get; } = Guid.NewGuid().ToString("N"); 
    public string Payload { get; } = Guid.NewGuid().ToString("N");
    public List<NestedClass> Children { get; } = new ();
}