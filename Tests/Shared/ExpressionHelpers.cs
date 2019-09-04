using System;
using System.Linq.Expressions;

namespace Qx.Helpers
{
    internal static class ExpressionHelpers
    {
        public static Expression<Func<TResult>> Expr<TResult>(Expression<Func<TResult>> expression) => expression;
        public static Expression<Func<TArg0, TResult>> Expr<TArg0, TResult>(Expression<Func<TArg0, TResult>> expression) => expression;
    }
}
