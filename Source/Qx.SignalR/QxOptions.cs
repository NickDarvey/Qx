using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Qx.Security.FeaturesVerification;
using static Qx.Security.MembersVerification;

namespace Qx.SignalR
{
    public class QxOptions
    {
        public static readonly QxOptions Default = new QxOptions(
            allowedFeatures: ExpressionFeatures.BasicExpressions |
                             ExpressionFeatures.Invocations |
                             ExpressionFeatures.TypeConversions |
                             ExpressionFeatures.TypeTests,
            allowedMembers: PrimitiveTypes.Concat(
                            PrimitiveMembers).Concat(
                            ExtendedPrimitiveTypes).Concat(
                            ExtendedPrimitiveMembers).Concat(
                            OperatorMembers).Concat(
                            TupleTypes).Concat(
                            TupleMembers));

        public QxOptions(ExpressionFeatures allowedFeatures, IEnumerable<MemberInfo> allowedMembers)
        {
            AllowedFeatures = allowedFeatures;
            AllowedMembers = allowedMembers;
        }

        public ExpressionFeatures AllowedFeatures { get; }
        public IEnumerable<MemberInfo> AllowedMembers { get; }

        public QxOptions WithAllowedFeatures(ExpressionFeatures allowedFeatures) =>
            new QxOptions(AllowedFeatures | allowedFeatures, AllowedMembers);

        public QxOptions WithAllowedMembers(IEnumerable<MemberInfo> members) =>
            new QxOptions(AllowedFeatures, AllowedMembers.Concat(members));
    }
}
