// -----------------------------------------------------------------------
//  <copyright file="AkkaHostingExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Hosting;

namespace Akka.Serialization.Compressed.Json;

public static class AkkaHostingExtensions
{
    public static AkkaConfigurationBuilder WithCompressedJsonSerializer(this AkkaConfigurationBuilder builder)
    {
        return builder.WithCustomSerializer(
            serializerIdentifier: "json-gzip",
            boundTypes: new[] { typeof(IShouldCompress) },
            serializerFactory: sys => new CompressedJsonSerializer(sys));
    }
    
    public static AkkaConfigurationBuilder WithCompressedJsonSerializer(this AkkaConfigurationBuilder builder, params Type[] boundTypes)
    {
        var typeHash = boundTypes.ToHashSet();
        typeHash.Add(typeof(IShouldCompress));
        
        return builder.WithCustomSerializer(
            serializerIdentifier: "json-gzip",
            boundTypes: typeHash,
            serializerFactory: sys => new CompressedJsonSerializer(sys));
    }
}