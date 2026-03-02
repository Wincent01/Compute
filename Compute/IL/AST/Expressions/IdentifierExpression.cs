using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    public enum IdentifierType
    {
        Variable,
        Parameter
    }
    
    /// <summary>
    /// Represents an identifier (variable, parameter, etc.)
    /// </summary>
    public class IdentifierExpression : ExpressionBase
    {
        public string Name { get; }

        public IdentifierType IdentifierType { get; }

        public int Index { get; }

        public IdentifierExpression(string name, IdentifierType identifierType, int index, AstType type) : base(type)
        {
            Name = name;
            IdentifierType = identifierType;
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
            return $"Identifier({Name})";
        }
    }
}