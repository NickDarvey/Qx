using System;
using System.Linq;

namespace Qx
{
    internal abstract class TypeVisitor
    {
        public virtual Type Visit(Type type) =>
            type.IsGenericType ? VisitGeneric(type)
            : type.IsGenericParameter ? VisitGenericParameter(type)
            : VisitOther(type);

        public virtual Type[] Visit(Type[] types)
        {
            var visitedTypes = default(Type[]);
            for (var i = 0; i < types.Length; ++i)
            {
                var current = types[i];
                var visited = Visit(current);
                if (current != visited) (visitedTypes ??= types.ToArray())[i] = visited;
            }
            return visitedTypes ?? types;
        }
        protected virtual Type VisitGeneric(Type type) =>
            type.IsGenericTypeDefinition ? VisitOpenGeneric(type) : VisitClosedGeneric(type);

        protected virtual Type VisitGenericParameter(Type type) => type; // ?

        protected virtual Type VisitClosedGeneric(Type type)
        {
            var genericType = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            var visitedGenericType = Visit(genericType);
            var visitedArguments = Visit(arguments);

            if (genericType != visitedGenericType ||
                arguments != visitedArguments)
                return visitedGenericType.MakeGenericType(visitedArguments);

            return type;
        }

        protected virtual Type VisitOpenGeneric(Type type) => type; // ?

        protected virtual Type VisitOther(Type type) => type;

        // What about arrays etc?
    }
}
