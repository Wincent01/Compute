using System.Collections.Generic;
using System.Linq;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Types of binary operations
    /// </summary>
    public enum BinaryOperatorType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        LeftShift,
        RightShift,
        LogicalAnd,
        LogicalOr,
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    /// <summary>
    /// Represents a binary operation expression
    /// </summary>
    public class BinaryExpression : ExpressionBase
    {
        public IExpression Left { get; }
        public BinaryOperatorType Operator { get; }
        public IExpression Right { get; }

        public override IEnumerable<IAstNode> Children => new IAstNode[] { Left, Right };

        public BinaryExpression(IExpression left, BinaryOperatorType op, IExpression right, AstType resultType) 
            : base(resultType)
        {
            Left = left;
            Operator = op;
            Right = right;
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
            return $"Binary({Left} {Operator} {Right})";
        }

        public static string GetOperatorSymbol(BinaryOperatorType op)
        {
            return op switch
            {
                BinaryOperatorType.Add => "+",
                BinaryOperatorType.Subtract => "-",
                BinaryOperatorType.Multiply => "*",
                BinaryOperatorType.Divide => "/",
                BinaryOperatorType.Modulo => "%",
                BinaryOperatorType.BitwiseAnd => "&",
                BinaryOperatorType.BitwiseOr => "|",
                BinaryOperatorType.BitwiseXor => "^",
                BinaryOperatorType.LeftShift => "<<",
                BinaryOperatorType.RightShift => ">>",
                BinaryOperatorType.LogicalAnd => "&&",
                BinaryOperatorType.LogicalOr => "||",
                BinaryOperatorType.Equal => "==",
                BinaryOperatorType.NotEqual => "!=",
                BinaryOperatorType.LessThan => "<",
                BinaryOperatorType.LessThanOrEqual => "<=",
                BinaryOperatorType.GreaterThan => ">",
                BinaryOperatorType.GreaterThanOrEqual => ">=",
                _ => "?"
            };
        }
    }
}