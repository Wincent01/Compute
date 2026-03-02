using System.Collections.Generic;
using System.Linq;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// A general-purpose AST rewriting visitor that clones/transforms the tree bottom-up.
    /// Override specific Visit* methods to intercept and transform individual node types.
    /// By default, each node is reconstructed with its children recursively rewritten.
    /// </summary>
    public class AstRewriter
    {
        // ── Expressions ──────────────────────────────────────────────

        public virtual IExpression Rewrite(IExpression expression)
        {
            return expression switch
            {
                LiteralExpression e       => RewriteLiteral(e),
                IdentifierExpression e    => RewriteIdentifier(e),
                BinaryExpression e        => RewriteBinary(e),
                UnaryExpression e         => RewriteUnary(e),
                CastExpression e          => RewriteCast(e),
                FieldAccessExpression e   => RewriteFieldAccess(e),
                ArrayAccessExpression e   => RewriteArrayAccess(e),
                FunctionCallExpression e  => RewriteFunctionCall(e),
                AddressOfExpression e     => RewriteAddressOf(e),
                DereferenceExpression e   => RewriteDereference(e),
                _ => expression // Unknown expression types pass through unchanged
            };
        }

        // ── Statements ───────────────────────────────────────────────

        public virtual IStatement Rewrite(IStatement statement)
        {
            return statement switch
            {
                BlockStatement s              => RewriteBlock(s),
                AssignmentStatement s          => RewriteAssignment(s),
                VariableDeclarationStatement s => RewriteVariableDeclaration(s),
                ExpressionStatement s          => RewriteExpressionStatement(s),
                ReturnStatement s              => RewriteReturn(s),
                BranchStatement s              => RewriteBranch(s),
                LabelStatement s               => RewriteLabel(s),
                CommentStatement s             => RewriteComment(s),
                NopStatement s                 => RewriteNop(s),
                _ => statement // Unknown statement types pass through unchanged
            };
        }

        // ── Expression rewrite methods (override to customize) ───────

        protected virtual IExpression RewriteLiteral(LiteralExpression node)
        {
            return node; // Leaf node, nothing to rewrite
        }

        protected virtual IExpression RewriteIdentifier(IdentifierExpression node)
        {
            return node; // Leaf node, nothing to rewrite
        }

        protected virtual IExpression RewriteBinary(BinaryExpression node)
        {
            var left = Rewrite(node.Left);
            var right = Rewrite(node.Right);

            if (ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right))
                return node;

            return new BinaryExpression(left, node.Operator, right, node.Type);
        }

        protected virtual IExpression RewriteUnary(UnaryExpression node)
        {
            var operand = Rewrite(node.Operand);

            if (ReferenceEquals(operand, node.Operand))
                return node;

            return new UnaryExpression(node.Operator, operand, node.Type);
        }

        protected virtual IExpression RewriteCast(CastExpression node)
        {
            var expr = Rewrite(node.Expression);

            if (ReferenceEquals(expr, node.Expression))
                return node;

            return new CastExpression(expr, node.TargetType);
        }

        protected virtual IExpression RewriteFieldAccess(FieldAccessExpression node)
        {
            var target = Rewrite(node.Target);

            if (ReferenceEquals(target, node.Target))
                return node;

            return new FieldAccessExpression(target, node.FieldName, node.Type);
        }

        protected virtual IExpression RewriteArrayAccess(ArrayAccessExpression node)
        {
            var array = Rewrite(node.Array);
            var index = Rewrite(node.Index);

            if (ReferenceEquals(array, node.Array) && ReferenceEquals(index, node.Index))
                return node;

            return new ArrayAccessExpression(array, index, node.Type);
        }

        protected virtual IExpression RewriteFunctionCall(FunctionCallExpression node)
        {
            var changed = false;
            var newArgs = new List<IExpression>(node.Arguments.Count);

            foreach (var arg in node.Arguments)
            {
                var rewritten = Rewrite(arg);
                newArgs.Add(rewritten);
                if (!ReferenceEquals(rewritten, arg))
                    changed = true;
            }

            if (!changed)
                return node;

            return new FunctionCallExpression(node.Method, newArgs, node.Type);
        }

        protected virtual IExpression RewriteAddressOf(AddressOfExpression node)
        {
            var expr = Rewrite(node.Expression);

            if (ReferenceEquals(expr, node.Expression))
                return node;

            return new AddressOfExpression(expr, node.TargetType);
        }

        protected virtual IExpression RewriteDereference(DereferenceExpression node)
        {
            var expr = Rewrite(node.Expression);

            if (ReferenceEquals(expr, node.Expression))
                return node;

            return new DereferenceExpression(expr, node.TargetType);
        }

        // ── Statement rewrite methods (override to customize) ────────

        protected virtual IStatement RewriteBlock(BlockStatement node)
        {
            var changed = false;
            var newStatements = new List<IStatement>(node.Statements.Count);

            foreach (var stmt in node.Statements)
            {
                var rewritten = Rewrite(stmt);
                newStatements.Add(rewritten);
                if (!ReferenceEquals(rewritten, stmt))
                    changed = true;
            }

            if (!changed)
                return node;

            return new BlockStatement(newStatements);
        }

        protected virtual IStatement RewriteAssignment(AssignmentStatement node)
        {
            var target = Rewrite(node.Target);
            var value = Rewrite(node.Value);

            if (ReferenceEquals(target, node.Target) && ReferenceEquals(value, node.Value))
                return node;

            return new AssignmentStatement(target, value);
        }

        protected virtual IStatement RewriteVariableDeclaration(VariableDeclarationStatement node)
        {
            if (node.InitialValue == null)
                return node;

            var init = Rewrite(node.InitialValue);

            if (ReferenceEquals(init, node.InitialValue))
                return node;

            return new VariableDeclarationStatement(node.Type, node.Name, init);
        }

        protected virtual IStatement RewriteExpressionStatement(ExpressionStatement node)
        {
            var expr = Rewrite(node.Expression);

            if (ReferenceEquals(expr, node.Expression))
                return node;

            return new ExpressionStatement(expr);
        }

        protected virtual IStatement RewriteReturn(ReturnStatement node)
        {
            if (node.Value == null)
                return node;

            var value = Rewrite(node.Value);

            if (ReferenceEquals(value, node.Value))
                return node;

            return new ReturnStatement(value);
        }

        protected virtual IStatement RewriteBranch(BranchStatement node)
        {
            if (node.Condition == null)
                return node;

            var condition = Rewrite(node.Condition);

            if (ReferenceEquals(condition, node.Condition))
                return node;

            return new BranchStatement(condition, node.TargetOffset);
        }

        protected virtual IStatement RewriteLabel(LabelStatement node)
        {
            return node; // Leaf node
        }

        protected virtual IStatement RewriteComment(CommentStatement node)
        {
            return node; // Leaf node
        }

        protected virtual IStatement RewriteNop(NopStatement node)
        {
            return node; // Leaf node
        }
    }
}
