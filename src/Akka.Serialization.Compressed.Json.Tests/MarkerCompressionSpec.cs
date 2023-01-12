// -----------------------------------------------------------------------
//  <copyright file="MarkerCompressionSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Serialization.Compressed.Json.Tests;

public class MarkerCompressionSpec: Akka.TestKit.Xunit2.TestKit
{
    public MarkerCompressionSpec(ITestOutputHelper helper)
        : base(CompressedJsonSerializer.DefaultConfiguration(), nameof(MarkerCompressionSpec), helper)
    {
    }

    [Fact(DisplayName = "Class marked with IShouldCompress should be compressed")]
    public void MarkedClassTest()
    {
        var data = new MarkedBigData();
        var serializer = Sys.Serialization.FindSerializerFor(data);
        serializer.Should().BeOfType<CompressedJsonSerializer>();
        var compressed = serializer.ToBinary(data);
        compressed.Length.Should().BeLessThan(10 * 1024);
    }
    
    [Fact(DisplayName = "Class not marked with IShouldCompress should not be compressed")]
    public void UnmarkedClassTest()
    {
        var data = new BigData();
        var serializer = Sys.Serialization.FindSerializerFor(data);
        serializer.Should().NotBeOfType<CompressedJsonSerializer>();
        var compressed = serializer.ToBinary(data);
        compressed.Length.Should().BeGreaterThan(data.Message.Length);
    }
}