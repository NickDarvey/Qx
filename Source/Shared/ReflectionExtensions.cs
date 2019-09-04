using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Qx.Internals
{
    internal static class ReflectionExtensions
    {
        public static MemberInfo GetMemberInfo<TResult>(Expression<Func<TResult>> expression) => GetMemberInfo(expression.Body);

        private static MemberInfo GetMemberInfo(Expression expression) =>
            expression switch
            {
                MethodCallExpression methodCall => methodCall.Method,
                _ => throw new NotSupportedException($"{expression.GetType().Name} does not contain a MethodInfo"),
            };

        public static MethodInfo GetMethodInfo<TResult>(Expression<Func<TResult>> expression) => GetMethodInfo(expression.Body);

        private static MethodInfo GetMethodInfo(Expression expression) =>
            expression switch
            {
                MethodCallExpression methodCall => methodCall.Method,
                _ => throw new NotSupportedException($"{expression.GetType().Name} does not contain a MethodInfo"),
            };

        public static object GetValue(this MemberInfo member, object instance) =>
            member.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)member).GetValue(instance, null),
                MemberTypes.Field => ((FieldInfo)member).GetValue(instance),
                _ => throw new InvalidOperationException($"Unknown MemberInfo {member.GetType().Name}"),
            };

        public static void SetValue(this MemberInfo member, object instance, object value)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    ((PropertyInfo)member).SetValue(instance, value, null);
                    break;
                case MemberTypes.Field:
                    ((FieldInfo)member).SetValue(instance, value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown MemberInfo {member.GetType().Name}");
            }
        }

        public static bool IsNullableType(this Type type) =>
            type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static bool IsNullAssignable(this Type type) =>
            !type.IsValueType || IsNullableType(type);

        public static Type GetNonNullableType(this Type type) =>
            IsNullableType(type) ? type.GetGenericArguments()[0] : type;

        public static Type[] GetParameterTypes(this MethodBase method)
        {
            var parameters = method.GetParameters();
            var parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) parameterTypes[i] = parameters[i].ParameterType;
            return parameterTypes;
        }

        public static bool TryGetDelegateTypes(this Type type, [NotNullWhen(true)] out Type[]? parameterTypes, [NotNullWhen(true)] out Type? returnType)
        {
            if (typeof(Delegate).IsAssignableFrom(type))
            {
                var method = type.GetMethod("Invoke");
                parameterTypes = method.GetParameterTypes();
                returnType = method.ReturnType;
                return true;
            }

            parameterTypes = default;
            returnType = default;
            return false;
        }

        // https://stackoverflow.com/a/1650895/1259408
        public static bool IsAnonymousType(this Type type) =>
            (type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal) || type.Name.StartsWith("VB$AnonymousType", StringComparison.Ordinal))
            && type.IsDefined(typeof(CompilerGeneratedAttribute), false);
    }
}
