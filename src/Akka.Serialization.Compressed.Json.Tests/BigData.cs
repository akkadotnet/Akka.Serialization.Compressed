// -----------------------------------------------------------------------
//  <copyright file="BigData.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Serialization.Compressed.Json.Tests;

public class BigData
{
    public string Message { get; } = new('x', 5 * 1024 * 1024); // 5 megabyte worth of chars
}

public class MarkedBigData: IShouldCompress
{
    public string Message { get; } = new('x', 5 * 1024 * 1024); // 5 megabyte worth of chars
}
