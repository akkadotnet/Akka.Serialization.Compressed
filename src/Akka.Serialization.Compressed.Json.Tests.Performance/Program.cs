using System.Reflection;
using BenchmarkDotNet.Running;

namespace Akka.Serialization.Compressed.Json.Tests.Performance;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
    }
}