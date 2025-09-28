using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents an identifier (variable, parameter, etc.)
    /// </summary>
    public class IdentifierExpression : ExpressionBase
    {
        public string Name { get; }

        public IdentifierExpression(string name, AstType type) : base(type)
        {
            Name = name;
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
            return $"Identifier({Name})";
        }
    }
}