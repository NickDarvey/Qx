using BenchmarkDotNet.Attributes;
using Qx.Prelude;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Qx.Benchmarks.Prelude
{
    [MemoryDiagnoser]
    [HtmlExporter]
    public class ValidationBenchmarks
    {
        private static readonly Validation<string, int> _subject = new Validation<string, int>(1);

        /// <summary>
        /// No select, as if we didn't have the safety of hiding the inner value.
        /// </summary>
        [Benchmark]
        public Validation<string, int> Select0()
        {
            if (_subject.IsValid) return new Validation<string, int>(1 * 2);
            else return new Validation<string, int>(Enumerable.Empty<string>());
        }

        /// <summary>
        /// The select as implemented.
        /// </summary>
        [Benchmark(Baseline = true)]
        public Validation<string, int> Select()
        {
            return _subject.Select(x => x * 2);
        }

        /// <summary>
        /// The select as if implemented using match.
        /// </summary>
        [Benchmark]
        public Validation<string, int> Select2()
        {
            return Select2(_subject, x => x * 2);
        }

        /// <summary>
        /// The select as if implemented using match and local functions.
        /// </summary>
        [Benchmark]
        public Validation<string, int> Select3()
        {
            return Select3(_subject, x => x * 2);
        }

        private static Validation<TError, TReturnValue> Select2<TError, TValue, TReturnValue>(Validation<TError, TValue> @this, Func<TValue, TReturnValue> select) =>
            @this.Match(Valid: value => new Validation<TError, TReturnValue>(select(value)), Invalid: errors => new Validation<TError, TReturnValue>(errors));

        private static Validation<TError, TReturnValue> Select3<TError, TValue, TReturnValue>(Validation<TError, TValue> @this, Func<TValue, TReturnValue> select)
        {
            Validation<TError, TReturnValue> OnValid(TValue value) =>
                new Validation<TError, TReturnValue>(select(value));

            static Validation<TError, TReturnValue> OnInvalid(IEnumerable<TError> errors) =>
                new Validation<TError, TReturnValue>(errors);

            return @this.Match(Valid: OnValid, Invalid: OnInvalid);

        }
    }
}
