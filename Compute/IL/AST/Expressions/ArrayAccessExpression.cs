using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents an array access expression
    /// </summary>
    public class ArrayAccessExpression : ExpressionBase
    {
        public IExpression Array { get; }
        public IExpression Index { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Array, Index };

        public ArrayAccessExpression(IExpression array, IExpression index, AstType elementType) 
            : base(elementType)
        {
            Array = array;
            Index = index;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"ArrayAccess({Array}[{Index}])";
        }
    }
}