using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Qx
{
    public static class Security
    {
        public delegate bool Verifier(Expression expression, out IEnumerable<string> errors);

        public static Verifier CreateVerifier(IEnumerable<MethodInfo> knownMethods, IEnumerable<Type> knownTypes, IEnumerable<Type> knownExtendedPrimitiveTypes)
        {
            var isVerifiedExtendedPrimitiveType = CreateExtendedPrimitiveTypeVerifier(knownExtendedPrimitiveTypes);
            var isVerifiedType = CreateTypeVerifier(knownTypes, isVerifiedExtendedPrimitiveType);
            var isVerifiedMethod = CreateMethodVerifier(knownMethods, isVerifiedExtendedPrimitiveType);

            bool Verify(Expression expression, out IEnumerable<string> errors)
            {
                var visitor = new Impl(isVerifiedMethod, isVerifiedType);
                _ = visitor.Visit(expression);

                if (visitor.Errors.Count == 0)
                {
                    errors = default;
                    return true;
                }

                else
                {
                    errors = visitor.Errors;
                    return false;
                }
            }

            return Verify;
        }

        private static readonly IEnumerable<MethodInfo> _operatorMethods =
            typeof(AsyncEnumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).Concat(
            typeof(AsyncQueryable).GetMethods(BindingFlags.Public | BindingFlags.Static));

        private static readonly HashSet<string> _knownOperatorMethodNames = new HashSet<string>()
        {
            // AsyncEnumerable/AsyncQuerable operators are homomorphic,
            // so we just use those from AsyncEnumerable.

            // Disallow operators that use buffering which is potentially unbounded.

            nameof(AsyncEnumerable.AllAsync),
            nameof(AsyncEnumerableEx.Amb),
            nameof(AsyncEnumerable.AnyAsync),
            nameof(AsyncEnumerable.Append),
            nameof(AsyncEnumerable.AverageAsync),
            //nameof(AsyncEnumerableEx.Buffer),
            //nameof(AsyncEnumerableEx.Catch), ?
            nameof(AsyncEnumerable.Concat),
            nameof(AsyncEnumerable.ContainsAsync),
            nameof(AsyncEnumerable.CountAsync),
            nameof(AsyncEnumerable.DefaultIfEmpty),
            //nameof(AsyncEnumerable.Distinct),
            //nameof(AsyncEnumerableEx.DistinctUntilChanged), ?
            nameof(AsyncEnumerable.ElementAtAsync),
            nameof(AsyncEnumerable.ElementAtOrDefaultAsync),
            //nameof(AsyncEnumerable.GroupBy),
            //nameof(AsyncEnumerable.GroupJoin),
            nameof(AsyncEnumerableEx.IgnoreElements),
            nameof(AsyncEnumerableEx.IsEmptyAsync),
            //nameof(AsyncEnumerable.Join),
            nameof(AsyncEnumerable.LastAsync),
            nameof(AsyncEnumerable.LastOrDefaultAsync),
            nameof(AsyncEnumerable.LongCountAsync),
            nameof(AsyncEnumerable.MaxAsync),
            nameof(AsyncEnumerableEx.MaxByAsync),
            nameof(AsyncEnumerableEx.Merge),
            nameof(AsyncEnumerable.MinAsync),
            nameof(AsyncEnumerableEx.MinByAsync),
            //nameof(AsyncEnumerableEx.OnErrorResumeNext), ?
            nameof(AsyncEnumerable.Range), // Constrain?
            nameof(AsyncEnumerableEx.Repeat), // Constrain?
            nameof(AsyncEnumerableEx.Return),
            //nameof(AsyncEnumerableEx.Retry), ?
            nameof(AsyncEnumerableEx.Scan),
            nameof(AsyncEnumerable.Select),
            nameof(AsyncEnumerable.SelectMany),
            nameof(AsyncEnumerable.SequenceEqualAsync),
            nameof(AsyncEnumerable.SingleAsync),
            nameof(AsyncEnumerable.SingleOrDefaultAsync),
            nameof(AsyncEnumerable.Skip),
            nameof(AsyncEnumerable.SkipLast),
            nameof(AsyncEnumerable.SkipWhile),
            nameof(AsyncEnumerableEx.StartWith),
            nameof(AsyncEnumerable.SumAsync),
            nameof(AsyncEnumerable.Take),
            nameof(AsyncEnumerable.TakeLast),
            nameof(AsyncEnumerable.TakeWhile),
            nameof(AsyncEnumerableEx.Timeout),
            //nameof(AsyncEnumerable.Intersect),
            //nameof(AsyncEnumerable.Union),
            nameof(AsyncEnumerable.Where),
            nameof(AsyncEnumerable.Zip),
        };

        public static readonly IEnumerable<MethodInfo> DefaultKnownMethods = _operatorMethods
          .Where(method => _knownOperatorMethodNames.Contains(method.Name))
          .ToList(); // Evaluate once

        public static readonly IEnumerable<Type> DefaultKnownExtendedPrimitiveTypes = new[]
        {
            typeof(Guid),
            typeof(Uri),
            typeof(TimeSpan),
            typeof(DateTimeOffset),
        };

        public static readonly IEnumerable<Type> DefaultKnownTypes = Enumerable.Empty<Type>();

        public static readonly Verifier DefaultVerifier = CreateVerifier(
            knownMethods: DefaultKnownMethods, knownTypes: DefaultKnownTypes, knownExtendedPrimitiveTypes: DefaultKnownExtendedPrimitiveTypes);

        private class Impl : ExpressionVisitor
        {
            public readonly List<string> Errors = new List<string>();

            private readonly MethodVerifier _isVerifiedMethod;
            private readonly TypeVerifier _isVerifiedType;

            public Impl(MethodVerifier isVerifiedMethod, TypeVerifier isVerifiedType)
            {
                _isVerifiedMethod = isVerifiedMethod;
                _isVerifiedType = isVerifiedType;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (_isVerifiedMethod(node.Method) == false) Errors.Add($"Calls to method {node.Method} are not allowed");
                return base.VisitMethodCall(node);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (_isVerifiedType(node.Type) == false) Errors.Add($"Type {node.Type} is not allowed");
                return base.VisitConstant(node);
            }

            protected override Expression VisitNew(NewExpression node)
            {
                var type = node.Constructor.DeclaringType;
                if (_isVerifiedType(type) == false) Errors.Add($"New instances of type {type} is not allowed");
                return base.VisitNew(node);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.AddAssign:
                    case ExpressionType.AddAssignChecked:
                    case ExpressionType.AndAssign:
                    case ExpressionType.Assign:
                    case ExpressionType.DivideAssign:
                    case ExpressionType.ExclusiveOrAssign:
                    case ExpressionType.LeftShiftAssign:
                    case ExpressionType.ModuloAssign:
                    case ExpressionType.MultiplyAssign:
                    case ExpressionType.MultiplyAssignChecked:
                    case ExpressionType.OrAssign:
                    case ExpressionType.PostDecrementAssign:
                    case ExpressionType.PostIncrementAssign:
                    case ExpressionType.PowerAssign:
                    case ExpressionType.PreDecrementAssign:
                    case ExpressionType.PreIncrementAssign:
                    case ExpressionType.RightShiftAssign:
                    case ExpressionType.SubtractAssign:
                    case ExpressionType.SubtractAssignChecked:
                        Errors.Add($"Assignments are not allowed");
                        break;
                }
                return base.VisitBinary(node);
            }

            protected override Expression VisitExtension(Expression node)
            {
                Errors.Add($"Extensions are not allowed");
                return base.VisitExtension(node);
            }
        }

        private delegate bool MethodVerifier(MethodInfo method);
        private static MethodVerifier CreateMethodVerifier(IEnumerable<MethodInfo> knownMethods, ExtendedPrimitiveTypeVerifier isVerifiedExtendedPrimitiveType) =>
            method => method.DeclaringType.IsEnum ? true
                    : method.DeclaringType.IsPrimitive ? true
                    : isVerifiedExtendedPrimitiveType(method.DeclaringType) ? true
                    : knownMethods.Contains(method.IsGenericMethod ? method.GetGenericMethodDefinition() : method) ? true
                    : false;


        private delegate bool ExtendedPrimitiveTypeVerifier(Type type);
        private static ExtendedPrimitiveTypeVerifier CreateExtendedPrimitiveTypeVerifier(IEnumerable<Type> knownExtendedPrimitiveTypes) =>
            type => knownExtendedPrimitiveTypes.Contains(type.IsGenericType ? type.GetGenericTypeDefinition() : type) ? true
                  : false;

        private delegate bool TypeVerifier(Type type);
        private static TypeVerifier CreateTypeVerifier(IEnumerable<Type> knownTypes, ExtendedPrimitiveTypeVerifier isVerifiedExtendedPrimitiveType)
        {
            bool Verify(Type type) =>
                type.IsEnum ? true
              : isVerifiedExtendedPrimitiveType(type) ? true
              : type.IsPrimitive ? true
              : type.IsArray && Verify(type.GetElementType()) ? true
              : knownTypes.Contains(type.IsGenericType ? type.GetGenericTypeDefinition() : type);

            return Verify;
        }


    }
}
