using Nyse.Schema;
using Qx.Security;
using System.Linq;

namespace Nyse.Server
{
    internal static class Security
    {
        public static Verifier Verify =
            AllowedFeaturesVerification.Create(
                AllowedFeaturesVerification.ExpressionFeatures.BasicExpressions |
                AllowedFeaturesVerification.ExpressionFeatures.Invocations |
                AllowedFeaturesVerification.ExpressionFeatures.TypeConversions |
                AllowedFeaturesVerification.ExpressionFeatures.TypeTests).And(
            AllowedMembersVerification.Create(AllowedMembersVerification.CreateDeclaredMembersVerifier(
                AllowedMembersVerification.DefaultPrimitiveTypes,
                AllowedMembersVerification.DefaultPrimitiveMembers,
                AllowedMembersVerification.DefaultExtendedPrimitiveTypes,
                AllowedMembersVerification.DefaultExtendedPrimitiveMembers,
                AllowedMembersVerification.DefaultOperatorMembers,
                AllowedMembersVerification.DefaultTupleTypes,
                AllowedMembersVerification.DefaultTupleMembers,

                // Allow anything in our Nyse.Schema library
                from types in typeof(SharePrice).Assembly.GetTypes()
                from members in types.GetMembers()
                select members
            )));
    }
}
