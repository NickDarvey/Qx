using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Qx.Security
{
    public static partial class MembersVerification
    {
        /// <summary>
        /// Compares a MemberInfo by its module (assembly), metadata token and its type arguments.
        /// </summary>
        /// <remarks>
        /// <see cref="MemberInfo"/> overrides == for value comparison, but not
        /// <see cref="MemberInfo.Equals(object)"/> or <see cref="MemberInfo.GetHashCode"/>
        /// (see https://github.com/dotnet/corefx/blob/cd666bb681149f76b6e716057928d299c8f47272/src/Common/src/CoreLib/System/Reflection/MemberInfo.cs#L10)
        /// but we need this for use in a <see cref="HashSet{T}'"/>.
        /// Based on https://github.com/microsoft/referencesource/blob/e0bf122d0e52a42688b92bb4be2cfd66ca3c2f07/System.Data.Linq/SqlClient/Common/TypeSystem.cs#L232,
        /// and https://stackoverflow.com/q/13615927.
        /// </remarks>
        private class MemberInfoEqualityComparer : IEqualityComparer<MemberInfo>
        {
            public static IEqualityComparer<MemberInfo> Instance = new MemberInfoEqualityComparer();

            private MemberInfoEqualityComparer() { }

            public bool Equals(MemberInfo x, MemberInfo y) =>
                x == y ? true
                : x == null || y == null ? false
                : x.Module != y.Module || x.MetadataToken != y.MetadataToken ? false
                // If it's a type then the type args must also match
                : x is Type tx && y is Type ty && tx.GetGenericArguments().SequenceEqual(ty.GetGenericArguments()) == false ? false
                // If it's a method or constructor then the type args must also match
                : x is MethodBase mx && y is MethodBase my && mx.DeclaringType.GetGenericArguments().SequenceEqual(my.DeclaringType.GetGenericArguments()) == false ? false
                : x is MethodInfo mmx && y is MethodInfo mmy && mmx.GetGenericArguments().SequenceEqual(mmy.GetGenericArguments()) == false ? false
                // If we've got this far, they match
                : true;

            public int GetHashCode(MemberInfo obj) =>
                obj.Module.GetHashCode() +
                obj.MetadataToken * 17;
        }

        public static MemberVerifier CreateDeclaredMembersVerifier(params MemberInfo[] members) =>
            CreateDeclaredMembersVerifier(members.AsEnumerable());

        public static MemberVerifier CreateDeclaredMembersVerifier(params IEnumerable<MemberInfo>[] members) =>
            CreateDeclaredMembersVerifier(members.SelectMany(m => m));

        public static MemberVerifier CreateDeclaredMembersVerifier(IEnumerable<MemberInfo> members)
        {
            var members_ = new HashSet<MemberInfo>(members, MemberInfoEqualityComparer.Instance);

            bool VerifyType(Type type) =>
                type.IsGenericType
                && type.IsGenericTypeDefinition == false // Would this ever be in an expression tree?
                && members_.Contains(type.GetGenericTypeDefinition())
                && type.GetGenericArguments().All(t => members_.Contains(t) || VerifyType(t));

            bool VerifyMethod(MethodInfo method) =>
                method.IsGenericMethod
                && method.IsGenericMethodDefinition == false // Would this ever be in an expression tree?
                && members_.Contains(method.GetGenericMethodDefinition());

            bool VerifyConstructor(ConstructorInfo constructor) =>
                constructor.DeclaringType.IsGenericType
                && members_.Contains(MethodBase.GetMethodFromHandle(
                    constructor.MethodHandle,
                    constructor.DeclaringType.GetGenericTypeDefinition().TypeHandle));

            bool Verify(MemberInfo member) =>
                members_.Contains(member)
                || member is Type type && VerifyType(type)
                || member is MethodInfo method && VerifyMethod(method)
                || member is ConstructorInfo constructor && VerifyConstructor(constructor);

            return Verify;
        }

        private static readonly HashSet<string> _declaredOperatorMethodNames = new HashSet<string>()
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

        public static readonly IEnumerable<MethodInfo> OperatorMembers =
            new[] { typeof(AsyncEnumerable), typeof(AsyncEnumerableEx), typeof(AsyncQueryable), typeof(AsyncQueryableEx) }
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => _declaredOperatorMethodNames.Contains(method.Name));

        /// <summary>
        /// Primitive types are:
        /// <see cref="string"/>,
        /// <see cref="bool"/>,
        /// <see cref="byte"/>,
        /// <see cref="sbyte"/>,
        /// <see cref="short"/>,
        /// <see cref="ushort"/>,
        /// <see cref="int"/>,
        /// <see cref="uint"/>,
        /// <see cref="long"/>,
        /// <see cref="ulong"/>,
        /// <see cref="char"/>,
        /// <see cref="decimal"/>,
        /// <see cref="double"/>, and
        /// <see cref="float"/>.
        /// </summary>
        // TOTHINK: Consider not exposing this and forcing it upon users,
        // it's convenient but will be unchangeable. I'm not sure if I want it in the codebase.
        public static readonly IEnumerable<Type> PrimitiveTypes =
            new[]
            {
                typeof(string),
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(char),
                typeof(decimal),
                typeof(double),
                typeof(float),
            };

        /// <summary>
        /// Primitive members are members of the types:
        /// <see cref="string"/>,
        /// <see cref="bool"/>,
        /// <see cref="byte"/>,
        /// <see cref="sbyte"/>,
        /// <see cref="short"/>,
        /// <see cref="ushort"/>,
        /// <see cref="int"/>,
        /// <see cref="uint"/>,
        /// <see cref="long"/>,
        /// <see cref="ulong"/>,
        /// <see cref="char"/>,
        /// <see cref="decimal"/>,
        /// <see cref="double"/>, and
        /// <see cref="float"/>.
        /// </summary>
        public static readonly IEnumerable<MemberInfo> PrimitiveMembers =
            PrimitiveTypes.SelectMany(t => t.GetMembers());

        /// <summary>
        /// Extended primitive types are:
        /// <see cref="Uri"/>,
        /// <see cref="Guid"/>,
        /// <see cref="DateTime"/>,
        /// <see cref="DateTimeOffset"/>, and
        /// <see cref="TimeSpan"/>.
        /// </summary>
        // TOTHINK: Consider not exposing this and forcing it upon users,
        // it's convenient but will be unchangeable. I'm not sure if I want it in the codebase.
        public static readonly IEnumerable<Type> ExtendedPrimitiveTypes =
            new[]
            {
                typeof(Uri),
                typeof(Guid),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Tuple<,>),
            };

        /// <summary>
        /// Extended primitive members are members of the types:
        /// <see cref="Uri"/>,
        /// <see cref="Guid"/>,
        /// <see cref="DateTime"/>,
        /// <see cref="DateTimeOffset"/>, and
        /// <see cref="TimeSpan"/>.
        public static readonly IEnumerable<MemberInfo> ExtendedPrimitiveMembers =
            ExtendedPrimitiveTypes.SelectMany(t => t.GetMembers());

        public static readonly IEnumerable<Type> TupleTypes =
            new[]
            {
                typeof(ValueTuple),
                typeof(ValueTuple<>),
                typeof(ValueTuple<,>),
                typeof(ValueTuple<,,>),
                typeof(ValueTuple<,,,>),
                typeof(ValueTuple<,,,,>),
                typeof(ValueTuple<,,,,,>),
                typeof(ValueTuple<,,,,,,>),
                typeof(ValueTuple<,,,,,,,>),
                typeof(Tuple),
                typeof(Tuple<>),
                typeof(Tuple<,>),
                typeof(Tuple<,,>),
                typeof(Tuple<,,,>),
                typeof(Tuple<,,,,>),
                typeof(Tuple<,,,,,>),
                typeof(Tuple<,,,,,,>),
                typeof(Tuple<,,,,,,,>),
            };

        public static readonly IEnumerable<MemberInfo> TupleMembers =
            TupleTypes.SelectMany(t => t.GetMembers());
    }
}
