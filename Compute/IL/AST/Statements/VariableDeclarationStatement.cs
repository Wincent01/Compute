using System.Collections.Generic;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// Represents a variable declaration statement
    /// </summary>
    public class VariableDeclarationStatement : StatementBase
    {
        public AstType Type { get; }
        public string Name { get; }
        public IExpression? InitialValue { get; }

        public override IEnumerable<IAstNode> Children => 
            InitialValue != null ? [InitialValue] : System.Array.Empty<IAstNode>();

        public VariableDeclarationStatement(AstType type, string name, IExpression? initialValue)
        {
            Type = type;
            Name = name;
            InitialValue = initialValue;
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
            return InitialValue != null 
                ? $"VariableDecl({Type} {Name} = {InitialValue})"
                : $"VariableDecl({Type} {Name})";
        }
    }
}