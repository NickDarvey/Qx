using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Qx.Security
{
    public static class AllowedFeatures
    {
        [Flags]
        public enum ExpressionFeatures
        {
            BasicExpressions    = 0,
            Assignments         = 1 << 0,
            Blocks              = 1 << 1,
            CatchBlocks         = 1 << 2 | TryBlocks,
            Goto                = 1 << 3,
            Invocations         = 1 << 4,
            Loops               = 1 << 5,
            ArrayInstantiation  = 1 << 6,
            TryBlocks           = 1 << 7,
            TypeTests           = 1 << 8,
            TypeConversions     = 1 << 9,

            All                 = ~(-1 << 10)
        }

        public static Verifier Create(ExpressionFeatures features) =>
            Verification.CreateVerifierPattern(new AllowedFeaturesScanner(features).Scan);

        // Based on https://github.com/RxDave/Qactive/blob/6cd5a058082562128d51c50e3ac8bd393ea6015e/Source/Qactive/SecurityExpressionVisitor.cs#L7
        private class AllowedFeaturesScanner : ExpressionVisitor
        {
            private readonly ExpressionFeatures _features;

            public AllowedFeaturesScanner(ExpressionFeatures features)
            {
                _features = features;
            }

            private List<(ExpressionFeatures Feature, Expression Node)>? Errors { get; set; }

            private void Check(ExpressionFeatures feature, Expression node)
            {
                if (_features.HasFlag(feature)) return;
                Errors ??= new List<(ExpressionFeatures Feature, Expression Node)>();
                Errors.Add((feature, node));
            }

            public IEnumerable<string> Scan(Expression expression)
            {
                _ = Visit(expression);
                return Errors?.Select(error =>
                    $"{error.Node.GetType().Name} '{error.Node.ToCSharpString()}' is not allowed because feature '{error.Feature}' is not enabled")!;
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
                        Check(ExpressionFeatures.Assignments, node); // ? maybe control via members
                        break;
                }

                return base.VisitBinary(node);
            }

            protected override Expression VisitBlock(BlockExpression node)
            {
                Check(ExpressionFeatures.Blocks, node);
                return base.VisitBlock(node);
            }

            protected override CatchBlock VisitCatchBlock(CatchBlock node)
            {
                Check(ExpressionFeatures.CatchBlocks, node.Body);
                return base.VisitCatchBlock(node);
            }

            protected override Expression VisitGoto(GotoExpression node)
            {
                if (node.Kind == GotoExpressionKind.Goto) Check(ExpressionFeatures.Goto, node);
                return base.VisitGoto(node);
            }

            protected override Expression VisitInvocation(InvocationExpression node)
            {
                Check(ExpressionFeatures.Invocations, node);
                return base.VisitInvocation(node);
            }

            protected override Expression VisitLabel(LabelExpression node)
            {
                Check(ExpressionFeatures.Goto, node);
                return base.VisitLabel(node);
            }

            protected override Expression VisitLoop(LoopExpression node)
            {
                Check(ExpressionFeatures.Loops, node);
                return base.VisitLoop(node);
            }

            protected override Expression VisitNewArray(NewArrayExpression node)
            {
                Check(ExpressionFeatures.ArrayInstantiation, node);
                return base.VisitNewArray(node);
            }

            protected override Expression VisitTry(TryExpression node)
            {
                Check(ExpressionFeatures.TryBlocks, node);
                return base.VisitTry(node);
            }

            protected override Expression VisitTypeBinary(TypeBinaryExpression node)
            {
                Check(ExpressionFeatures.TypeTests, node);
                return base.VisitTypeBinary(node);
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.TypeAs:
                    case ExpressionType.TypeIs:
                        Check(ExpressionFeatures.TypeTests, node);
                        break;
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        // Conversions can be used as brute force type tests
                        Check(ExpressionFeatures.TypeTests | ExpressionFeatures.TypeConversions, node);
                        break;
                }

                return base.VisitUnary(node);
            }
        }
    }
}
