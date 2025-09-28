using System;
using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents a literal value expression
    /// </summary>
    public class LiteralExpression : ExpressionBase
    {
        public object Value { get; }

        public LiteralExpression(object value, AstType type) : base(type)
        {
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
            return $"Literal({Value})";
        }

        // Factory methods for common literal types
        public static LiteralExpression Int32(int value) => new(value, PrimitiveAstType.Int32);
        public static LiteralExpression UInt32(uint value) => new(value, PrimitiveAstType.UInt32);
        public static LiteralExpression Float32(float value) => new(value, PrimitiveAstType.Float32);
        public static LiteralExpression Boolean(bool value) => new(value, PrimitiveAstType.Boolean);
        public static LiteralExpression String(string value) => new(value, new PointerAstType(PrimitiveAstType.Int32)); // char*
    }
}