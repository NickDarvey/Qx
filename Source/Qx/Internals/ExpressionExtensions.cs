using ExpressionToString;
using System.Linq.Expressions;

namespace Qx.Internals
{
    internal static class ExpressionExtensions
    {
        public static string ToCSharpString(this Expression expression) => expression.ToString("C#");

    }
}
