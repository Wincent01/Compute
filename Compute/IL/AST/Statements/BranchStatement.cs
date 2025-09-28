using System.Collections.Generic;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents a branch statement (if)
    /// </summary>
    public class BranchStatement : StatementBase
    {
        public IExpression? Condition { get; }

        public int TargetOffset { get; }

        public BranchStatement(IExpression? condition, int targetOffset)
        {
            Condition = condition;
            TargetOffset = targetOffset;
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
            return $"Branch(Condition: {Condition}, TargetOffset: IL_{TargetOffset:0000})";
        }
    }
}