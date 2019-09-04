using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Qx.Internals.ReflectionExtensions;

namespace Qx.Internals
{
    internal static class KnownResourceExtensions
    {
        public static bool TryGetKnownResourceType(Type type, [NotNullWhen(true)] out Type? resourceType, [NotNullWhen(true)] out bool? isResourceInvocation)
        {
            if (IsKnownResourceType(type))
            {
                resourceType = type;
                isResourceInvocation = false;
                return true;
            }

            if (type.TryGetDelegateTypes(out _, out var returnType)
                && IsKnownResourceType(returnType))
            {
                resourceType = returnType;
                isResourceInvocation = true;
                return true;
            }

            resourceType = default;
            isResourceInvocation = default;
            return false;
        }


        public static bool IsKnownResourceType(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>);
    }
}
