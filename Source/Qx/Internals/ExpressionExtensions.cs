using ExpressionToString;
using ExpressionToString.Util;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Qx.Internals
{
    internal static class ExpressionExtensions
    {
        public static string ToCSharpString(this Expression expression) => expression.ToString("C#");

        public static string ToCSharpString(this MemberInfo member) =>
            member switch
            {
                ConstructorInfo constructor => constructor.ToCSharpString(),
                EventInfo @event => @event.ToCSharpString(),
                MethodInfo method => method.ToCSharpString(),
                PropertyInfo property => property.ToCSharpString(),
                Type type => type.ToCSharpString(),
                _ => member.ToString(), // ?
            };

        public static string ToCSharpString(this Type type) => type.FriendlyName("C#");

        public static string ToCSharpString(this ConstructorInfo constructor) =>
            $"new {constructor.DeclaringType.ToCSharpString()}({constructor.GetParameters().ToCSharpString()})";

        public static string ToCSharpString(this EventInfo @event) => @event.ToString(); // TODO

        public static string ToCSharpString(this FieldInfo field) =>
            $"{field.FieldType.ToCSharpString()} {field.DeclaringType.ToCSharpString()}.{field.Name}";

        public static string ToCSharpString(this MethodInfo method)
        {
            var methodName = method.IsGenericMethod
                ? $"{method.Name}<{string.Join(", ", method.GetGenericArguments().Select(t => t.ToCSharpString()))}>"
                : method.Name;
            return $"{method.ReturnType.ToCSharpString()} {method.DeclaringType.ToCSharpString()}.{methodName}({method.GetParameters().ToCSharpString()})";
        }

        public static string ToCSharpString(this PropertyInfo property)
        {
            var accessors = " { ";
            if (property.GetGetMethod() != null) accessors += " get; ";
            if (property.GetSetMethod() != null) accessors += " set; ";
            accessors += " } ";

            return $"{property.PropertyType.ToCSharpString()} {property.DeclaringType.ToCSharpString()}.{property.Name} {accessors}";
        }

        public static string ToCSharpString(this ParameterInfo[] parameters) =>
            string.Join(", ", parameters.Select(p => p.ParameterType.ToCSharpString() + " " + p.Name));

    }
}
