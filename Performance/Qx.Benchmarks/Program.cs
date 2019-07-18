using BenchmarkDotNet.Running;
using System;

namespace Qx.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Binders>();
        }
    }
}
