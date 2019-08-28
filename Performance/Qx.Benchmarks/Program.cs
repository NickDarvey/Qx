using BenchmarkDotNet.Running;
using Qx.Benchmarks.Security;
using System;

namespace Qx.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<QueriesBenchmarks>();
        }
    }
}
