using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Qx.Prelude;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Qx.Benchmarks
{
    [MemoryDiagnoser]
    public class BindersBenchmarks
    {
        private readonly Consumer _consumer = new Consumer();
        private static readonly ParameterExpression[] _parameters = new[]
        {
            Expression.Parameter(typeof(int), "Zero"),
            Expression.Parameter(typeof(long), "One"),
            Expression.Parameter(typeof(string), "Two"),
            Expression.Parameter(typeof(string), "Three"),
            Expression.Parameter(typeof(string), "Four"),
            Expression.Parameter(typeof(string), "Five"),
        };

        private static readonly Dictionary<string, string> _methods = new Dictionary<string, string>
        {
            { _parameters[0].Name, "0" },
            { _parameters[1].Name, "1" },
            { _parameters[2].Name, "2" },
            { _parameters[3].Name, "3" },
            { _parameters[4].Name, "4" },
            { _parameters[5].Name, "5" },
        };

        [Benchmark(Baseline = true)]
        public IReadOnlyDictionary<ParameterExpression, string> TryBindMethods()
        {
            _ = Qx.SignalR.Binders.TryBindMethods(_parameters, _methods, out var parameterMethodBindings, out _);
            return parameterMethodBindings;
        }

        [Benchmark]
        public void BindMethods()
        {
            Qx.SignalR.Binders.BindMethods(_parameters, _methods).Match(
                Valid: v => v, Invalid: e => throw new InvalidOperationException(string.Join(", ", e))).Consume(_consumer);
        }
    }
}
