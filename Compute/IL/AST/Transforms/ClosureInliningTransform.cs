using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;

namespace Compute.IL.AST.Transforms
{
    /// <summary>
    /// Inlines a closure by replacing all `this->field` accesses with direct parameter
    /// identifiers. This eliminates the need for the closure struct and the two-function
    /// pattern (inner function + __kernel wrapper), producing a single flat kernel where
    /// each captured variable becomes a direct kernel parameter.
    /// 
    /// Rewrite rules:
    ///   FieldAccess(this, "f")            → IdentifierExpression("f", Parameter)
    ///   AddressOf(FieldAccess(this, "f")) → AddressOf(IdentifierExpression("f", Parameter))
    ///   Assignment(FieldAccess(this, "f"), val)  → Assignment(Identifier("f"), val)
    ///   ArrayAccess(FieldAccess(this, "a"), idx) → ArrayAccess(Identifier("a"), idx)
    /// </summary>
    public class ClosureInliningTransform : IAstTransform
    {
        public BlockStatement Transform(BlockStatement body, AstTransformContext context)
        {
            if (context.ClosureFields == null || context.ClosureFields.Length == 0)
                return body; // Not a closure — nothing to do

            // Build a mapping from field name → replacement parameter identifier
            var fieldMap = new Dictionary<string, IdentifierExpression>();

            for (var i = 0; i < context.ClosureFields.Length; i++)
            {
                var field = context.ClosureFields[i];
                var astType = AstType.FromClrType(field.FieldType);

                // Arrays are passed as pointers in OpenCL
                if (astType is ArrayAstType)
                {
                    // Keep it as ArrayAstType — the code generator renders it as a pointer
                }

                fieldMap[field.Name] = new IdentifierExpression(
                    field.Name,
                    IdentifierType.Parameter,
                    i,
                    astType
                );
            }

            var rewriter = new ClosureRewriter(fieldMap);
            var result = rewriter.Rewrite(body);

            return (BlockStatement)result;
        }

        /// <summary>
        /// Specialized rewriter that replaces this->field patterns with direct parameter references.
        /// </summary>
        private class ClosureRewriter : AstRewriter
        {
            private readonly Dictionary<string, IdentifierExpression> _fieldMap;

            public ClosureRewriter(Dictionary<string, IdentifierExpression> fieldMap)
            {
                _fieldMap = fieldMap;
            }

            /// <summary>
            /// Core rewrite: FieldAccess where target is the "this" identifier
            /// gets replaced with the corresponding parameter identifier.
            /// </summary>
            protected override IExpression RewriteFieldAccess(FieldAccessExpression node)
            {
                // Check if this is a `this->field` pattern
                if (node.Target is IdentifierExpression { Name: "this" } &&
                    _fieldMap.TryGetValue(node.FieldName, out var replacement))
                {
                    return replacement;
                }

                // Not a closure field access — recurse normally
                return base.RewriteFieldAccess(node);
            }

            /// <summary>
            /// Handle AddressOf(FieldAccess(this, field)) by first rewriting the inner
            /// expression. If the inner FieldAccess was replaced with a parameter identifier,
            /// we still wrap it in AddressOf.
            /// </summary>
            protected override IExpression RewriteAddressOf(AddressOfExpression node)
            {
                var rewrittenInner = Rewrite(node.Expression);

                if (ReferenceEquals(rewrittenInner, node.Expression))
                    return node;

                // Rebuild with the rewritten inner expression and an appropriate pointer type
                var pointerType = rewrittenInner.Type is PointerAstType
                    ? rewrittenInner.Type
                    : new PointerAstType(rewrittenInner.Type);

                return new AddressOfExpression(rewrittenInner, pointerType);
            }
        }
    }
}
