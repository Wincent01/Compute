using System.Collections.Generic;
using System.Linq;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents a no-operation statement (used when an instruction doesn't produce a meaningful statement)
    /// </summary>
    public class NopStatement : StatementBase
    {
        public override IEnumerable<IAstNode> Children => Enumerable.Empty<IAstNode>();

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
            return "Nop()";
        }
    }
}