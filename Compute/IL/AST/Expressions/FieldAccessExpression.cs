using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents field access (struct.field or struct->field)
    /// </summary>
    public class FieldAccessExpression : ExpressionBase
    {
        public IExpression Target { get; }
        public string FieldName { get; }
        public override IEnumerable<IAstNode> Children => new IAstNode[] { Target };

        public FieldAccessExpression(IExpression target, string fieldName, AstType fieldType) 
            : base(fieldType)
        {
            Target = target;
            FieldName = fieldName;
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
            return $"FieldAccess({Target}.{FieldName})";
        }
    }
}