using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


namespace Qx.Security
{
    /// <summary>
    /// Verifies an expression tree for security errors.
    /// </summary>
    /// <param name="expression">The expression to verify.</param>
    /// <param name="errors">Security errors, or null.</param>
    /// <returns>True, if no security errors, else false.</returns>
    public delegate bool Verifier(Expression expression, out IEnumerable<string> errors);

    public static class Verification
    {
        public static Verifier And(this Verifier left, Verifier right)
        {
            bool Verify(Expression expression, out IEnumerable<string> errors)
            {
                // Evaluate both to collect all errors
                var left_ = left(expression, out var leftErrors);
                var right_ = right(expression, out var rightErrors);
                var verified = left_ && right_;
                errors = verified ? null
                       : left_ == false && right_ == true ? leftErrors
                       : left_ == true && right_ == false ? rightErrors
                       : leftErrors.Concat(rightErrors);
                return left_ && right_;
            }

            return Verify;
        }

        public static Verifier Or(this Verifier left, Verifier right)
        {
            bool Verify(Expression expression, out IEnumerable<string> errors)
            {
                var left_ = left(expression, out errors);
                if (left_) return true;
                else return right(expression, out errors);
            }

            return Verify;
        }

        internal static Verifier CreateVerifier(Func<Expression, IEnumerable<string>> scan)
        {
            bool Verify(Expression expression, out IEnumerable<string> errors)
            {
                var collected = scan(expression);

                if (collected == null)
                {
                    errors = default;
                    return true;
                }

                else
                {
                    errors = collected;
                    return false;
                }
            }

            return Verify;
        }
    }
    
}
