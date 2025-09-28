using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Types of unary operations
    /// </summary>
    public enum UnaryOperatorType
    {
        Plus,
        Minus,
        BitwiseNot,
        LogicalNot,
        Dereference,
        AddressOf
    }

    /// <summary>
    /// Represents a unary operation expression
    /// </summary>
    public class UnaryExpression : ExpressionBase
    {
        public UnaryOperatorType Operator { get; }
        public IExpression Operand { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Operand };

        public UnaryExpression(UnaryOperatorType op, IExpression operand, AstType resultType) 
            : base(resultType)
        {
            Operator = op;
            Operand = operand;
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
            return $"Unary({Operator} {Operand})";
        }

        public static string GetOperatorSymbol(UnaryOperatorType op)
        {
            return op switch
            {
                UnaryOperatorType.Plus => "+",
                UnaryOperatorType.Minus => "-",
                UnaryOperatorType.BitwiseNot => "~",
                UnaryOperatorType.LogicalNot => "!",
                UnaryOperatorType.Dereference => "*",
                UnaryOperatorType.AddressOf => "&",
                _ => "?"
            };
        }
    }
}