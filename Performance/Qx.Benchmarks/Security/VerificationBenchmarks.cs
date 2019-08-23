using BenchmarkDotNet.Attributes;
using Qx.Prelude;
using Qx.Security;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Qx.Benchmarks.Security
{
    [MemoryDiagnoser]
    public class VerificationBenchmarks
    {
        private Expression _expression;
        private Verifier[] _verifiers;

        private Verifier _target;
        private Verifier _target2;
        private Verifier _target3;
        private Verifier _target4;
        private Verifier _target5;

        [Params(2, 10)]
        public int VerifierCount;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _expression = Expression.Add(Expression.Constant(40), Expression.Constant(2));
            _verifiers = Enumerable.Repeat(
                FeaturesVerification.Create(FeaturesVerification.ExpressionFeatures.All), VerifierCount).ToArray();

            _target = Verification.Combine(_verifiers);
            _target2 = Combine2(_verifiers);
            _target3 = Combine3(_verifiers);
            _target4 = Combine4(_verifiers);
            _target5 = Combine5(_verifiers);
        }

        /// <summary>
        /// The combine method as implemented.
        /// </summary>
        [Benchmark(Baseline = true)]
        public Validation<string, Unit> Combine()
        {
            return _target(_expression);
        }

        /// <summary>
        /// The combine method using TraverseA.
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public Validation<string, Unit> Combine2()
        {
            return _target2(_expression);
        }

        private static Verifier Combine2(params Verifier[] verifiers) =>
            expression => from xs in verifiers.TraverseA(v => v(expression))
                          select Unit.Default;


        /// <summary>
        /// The combine method using match and apply.
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public Validation<string, Unit> Combine3()
        {
            return _target3(_expression);
        }

        private static Verifier Combine3(params Verifier[] verifiers) =>
            expression =>
            {
                var current = new Validation<string, Unit>(Unit.Default);
                foreach (var verify in verifiers)
                {
                    var next = verify(expression);
                    current = next.Match(
                        Valid: _ => current.Apply(new Validation<string, Func<Unit, Unit>>(_ => Unit.Default)),
                        Invalid: errors => current.Apply(new Validation<string, Func<Unit, Unit>>(errors)));
                }

                return current;
            };

        /// <summary>
        /// The combine method using apply only.
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public Validation<string, Unit> Combine4()
        {
            return _target4(_expression);
        }

        private static Verifier Combine4(params Verifier[] verifiers) =>
            expression =>
            {
                var current = new Validation<string, Unit>(Unit.Default);
                foreach (var verify in verifiers)
                {
                    var next = verify(expression);
                    current = next.Apply(current.Apply(new Validation<string, Func<Unit, Func<Unit, Unit>>>(l => r => r)));
                }

                return current;
            };

        /// <summary>
        /// The combine method using apply only, and caching values.
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public Validation<string, Unit> Combine5()
        {
            return _target5(_expression);
        }


        private static readonly Validation<string, Unit> Init = new Validation<string, Unit>(Unit.Default);
        private static readonly Validation<string, Func<Unit, Func<Unit, Unit>>> KeepRight = new Validation<string, Func<Unit, Func<Unit, Unit>>>(l => r => r);

        private static Verifier Combine5(params Verifier[] verifiers) =>
            expression =>
            {
                var current = Init;
                foreach (var verify in verifiers)
                {
                    var next = verify(expression);
                    current = next.Apply(current.Apply(KeepRight));
                }

                return current;
            };

    }
}

