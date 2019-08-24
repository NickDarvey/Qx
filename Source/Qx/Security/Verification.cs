using Qx.Prelude;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Qx.Security
{
    /// <summary>
    /// Verifies or refutes the security of an expression.
    /// </summary>
    /// <param name="expression">The expression to verify.</param>
    /// <returns>Valid, if verified. Invalid, if refuted.</returns>
    public delegate Validation<string, Unit> Verifier(Expression expression);

    public static class Verification
    {
        /// <summary>
        /// Creates a verifier based on a pattern of returning <see cref="IEnumerable{string}"/> if there are errors, else <see cref="null"/>.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        internal static Verifier CreatePatternedVerifier(Func<Expression, IEnumerable<string>?> func)
        {
            Validation<string, Unit> Verifier(Expression expression)
            {
                var results = func(expression);
                return results == null ? Verified : Refute(reasons: results);
            }

            return Verifier;
        }

        /// <summary>
        /// A validation that represents a verification of an expression
        /// </summary>
        public static readonly Validation<string, Unit> Verified = new Validation<string, Unit>(Unit.Default);

        /// <summary>
        /// A validation that represents an empty refutation of an expression.
        /// </summary>
        public static readonly Validation<string, Unit> Refuted = new Validation<string, Unit>();

        public static Validation<string, Unit> Refute(IEnumerable<string> reasons) => new Validation<string, Unit>(reasons);

        private static readonly Validation<string, Func<Unit, Func<Unit, Unit>>> KeepRight = new Validation<string, Func<Unit, Func<Unit, Unit>>>(l => r => r);

        public static Verifier Combine(params Verifier[] verifiers) =>
            expression =>
            {
                var current = Verified;
                foreach (var verify in verifiers)
                {
                    var next = verify(expression);
                    current = next.Apply(current.Apply(KeepRight));
                }

                return current;
            };
    }
}

