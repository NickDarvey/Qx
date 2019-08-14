using Qx.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Qx.Security
{

    /// <summary>
    /// Verifies <see cref="MemberInfo"/>s of an expression against <see cref="MemberVerifier"/>s.
    /// If any member verifiers return true, the member is allowed.
    /// </summary>
    public static partial class AllowedMembersVerification
    {
        public delegate bool MemberVerifier(MemberInfo member);

        public static Verifier Create(params MemberVerifier[] allowedMemberVerifiers) =>
            Verification.CreateVerifier(new AllowedMembersScanner(allowedMemberVerifiers).Scan);

        public static Verifier Create(IEnumerable<MemberVerifier> allowedMemberVerifiers) =>
            Verification.CreateVerifier(new AllowedMembersScanner(allowedMemberVerifiers.ToArray()).Scan);

        /// <summary>
        /// A default implementation of an <see cref="AllowedMembersVerification"/> <see cref="Verifier"/>.
        /// </summary>
        // TOTHINK: Should we force users to define these?
        public static readonly Verifier Verify = Create(CreateDeclaredMembersVerifier(
            DefaultPrimitiveTypes,
            DefaultPrimitiveMembers,
            DefaultExtendedPrimitiveTypes,
            DefaultExtendedPrimitiveMembers,
            DefaultOperatorMembers));
        
        private class AllowedMembersScanner : ExpressionVisitor
        {
            private readonly MemberVerifier[] _verifiers;

            public AllowedMembersScanner(MemberVerifier[] verifiers)
            {
                _verifiers = verifiers;
            }

            private List<(MemberInfo Member, Expression? Node)> Errors { get; set; }

            private void Check(MemberInfo member, Expression? node = null)
            {
                for (int i = 0; i < _verifiers.Length; i++)
                {
                    if (_verifiers[i](member)) return;
                }
                Errors ??= new List<(MemberInfo Member, Expression Subject)>();
                Errors.Add((member, node));
            }

            public IEnumerable<string> Scan(Expression expr)
            {
                _ = Visit(expr);
                return Errors?.Select(error =>
                    $"{error.Node?.GetType().Name} '{error.Node?.ToCSharpString()}' is not allowed because it uses {error.Member.MemberType} member '{error.Member.ToCSharpString()}' which is not declared.");
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.Method != default) Check(node.Method, node);
                return base.VisitBinary(node);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                // This expression visitor checks instances of members.
                // In the case of a ConstantExpression, it's an instance of a type.
                Check(node.Type, node);
                return base.VisitConstant(node);
            }

            protected override ElementInit VisitElementInit(ElementInit node)
            {
                Check(node.AddMethod);
                return base.VisitElementInit(node);
            }

            protected override Expression VisitIndex(IndexExpression node)
            {
                Check(node.Indexer, node);
                return base.VisitIndex(node);
            }

            /// <summary>
            /// Check collection or object instantiation.
            /// </summary>
            protected override MemberBinding VisitMemberBinding(MemberBinding node)
            {
                if (node.Member != default) Check(node.Member);
                return base.VisitMemberBinding(node);
            }

            /// <summary>
            /// Check fields and properties.
            /// </summary>
            protected override Expression VisitMember(MemberExpression node)
            {
                Check(node.Member, node);
                return base.VisitMember(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                Check(node.Method, node);
                return base.VisitMethodCall(node);
            }

            protected override Expression VisitNew(NewExpression node)
            {
                Check(node.Constructor, node);
                return base.VisitNew(node);
            }
        }
    }
}
