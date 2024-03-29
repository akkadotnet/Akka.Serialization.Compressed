﻿//-----------------------------------------------------------------------
// <copyright file="Configs.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

namespace Akka.Serialization.Compressed.Json.Tests.Performance
{
    /// <summary>
    /// Basic BenchmarkDotNet configuration used for micro benchmarks.
    /// </summary>
    public class MicroBenchmarkConfig : ManualConfig
    {
        public MicroBenchmarkConfig()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddLogger(ConsoleLogger.Default);
        }
    }

    /// <summary>
    /// BenchmarkDotNet configuration used for monitored jobs (not for micro benchmarks).
    /// </summary>
    public class MonitoringConfig : ManualConfig
    {
        public MonitoringConfig()
        {
            AddExporter(MarkdownExporter.GitHub);
        }
    }
}
