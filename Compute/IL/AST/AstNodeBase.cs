using System.Collections.Generic;
using System.Linq;

namespace Compute.IL.AST
{
    /// <summary>
    /// Base abstract class for all AST nodes
    /// </summary>
    public abstract class AstNodeBase : IAstNode
    {
        public virtual IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();

        public abstract T Accept<T>(IAstVisitor<T> visitor);
        
        public abstract void Accept(IAstVisitor visitor);
    }

    /// <summary>
    /// Base abstract class for expressions
    /// </summary>
    public abstract class ExpressionBase : AstNodeBase, IExpression
    {
        public AstType Type { get; protected set; }

        protected ExpressionBase(AstType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Base abstract class for statements
    /// </summary>
    public abstract class StatementBase : AstNodeBase, IStatement
    {
    }
}