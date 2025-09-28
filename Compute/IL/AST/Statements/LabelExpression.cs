using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents a label (for loops, conditionals, etc.)
    /// </summary>
    public class LabelStatement : StatementBase
    {
        public int Offset { get; }

        public LabelStatement(int offset)
        {
            Offset = offset;
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
            return $"Label({Offset})";
        }
    }
}