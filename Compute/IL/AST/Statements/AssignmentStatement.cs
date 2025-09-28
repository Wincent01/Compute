using System.Collections.Generic;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents an assignment statement
    /// </summary>
    public class AssignmentStatement : StatementBase
    {
        public IExpression Target { get; }
        public IExpression Value { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Target, Value };

        public AssignmentStatement(IExpression target, IExpression value)
        {
            Target = target;
            Value = value;
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
            return $"Assignment({Target} = {Value})";
        }
    }
}