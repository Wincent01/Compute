using System.Collections.Generic;
using System.Linq;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents a block of statements
    /// </summary>
    public class BlockStatement : StatementBase
    {
        public IReadOnlyList<IStatement> Statements { get; }

        public override IEnumerable<IAstNode> Children => Statements.Cast<IAstNode>();

        public BlockStatement(IReadOnlyList<IStatement> statements)
        {
            Statements = statements;
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
            return $"Block({Statements.Count} statements)";
        }
    }
}