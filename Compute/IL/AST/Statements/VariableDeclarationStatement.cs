using System.Collections.Generic;

namespace Compute.IL.AST.Statements
{
    /// <summary>
    /// OpenCL address space qualifiers for variable declarations.
    /// </summary>
    public enum AddressSpace
    {
        None,
        Global,
        Local,
        Constant,
        Private
    }

    /// <summary>
    /// Represents a variable declaration statement.
    /// Optionally carries an address space qualifier and fixed array size
    /// for declarations like <c>__local float tile[256]</c>.
    /// </summary>
    public class VariableDeclarationStatement : StatementBase
    {
        public AstType Type { get; }
        public string Name { get; }
        public IExpression? InitialValue { get; }

        /// <summary>
        /// OpenCL address space qualifier (__local, __global, etc.). Default is None.
        /// </summary>
        public AddressSpace AddressSpace { get; set; } = AddressSpace.None;

        /// <summary>
        /// Fixed array size for declarations like <c>__local float name[256]</c>.
        /// When set, the type's element type is used and the variable is declared as a fixed-size array.
        /// </summary>
        public int? ArraySize { get; set; }

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
            var prefix = AddressSpace != AddressSpace.None ? $"{AddressSpace} " : "";
            var suffix = ArraySize.HasValue ? $"[{ArraySize.Value}]" : "";
            return InitialValue != null 
                ? $"VariableDecl({prefix}{Type} {Name}{suffix} = {InitialValue})"
                : $"VariableDecl({prefix}{Type} {Name}{suffix})";
        }
    }
}