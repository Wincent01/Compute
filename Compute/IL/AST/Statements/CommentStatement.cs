using System.Collections.Generic;

namespace Compute.IL.AST.Expressions
{
    /// <summary>
    /// Represents a comment in the code
    /// </summary>
    public class CommentStatement : StatementBase
    {
        public string Comment { get; }

        public CommentStatement(string comment)
        {
            Comment = comment;
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
            return $"Comment({Comment})";
        }
    }
}