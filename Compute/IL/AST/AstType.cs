using System;

namespace Compute.IL.AST
{
    /// <summary>
    /// Represents type information in the AST
    /// </summary>
    public abstract class AstType
    {
        /// <summary>
        /// The .NET type this represents
        /// </summary>
        public Type? ClrType { get; protected set; }

        /// <summary>
        /// Whether this type is a pointer
        /// </summary>
        public virtual bool IsPointer => false;

        /// <summary>
        /// Whether this type is an array
        /// </summary>
        public virtual bool IsArray => false;

        /// <summary>
        /// Whether this type is a struct
        /// </summary>
        public virtual bool IsStruct => ClrType?.IsValueType ?? false;

        /// <summary>
        /// Whether this type is a primitive
        /// </summary>
        public virtual bool IsPrimitive => ClrType?.IsPrimitive ?? false;

        /// <summary>
        /// Whether this type is void
        /// </summary>
        public virtual bool IsVoid => ClrType == typeof(void);

        protected AstType(Type? clrType)
        {
            ClrType = clrType;
        }

        /// <summary>
        /// Convert to OpenCL type string representation
        /// </summary>
        public virtual string ToOpenClString()
        {
            return ClrType?.Name ?? "Unknown";
        }

        public override string ToString()
        {
            return ClrType?.Name ?? "Unknown";
        }

        public static AstType FromClrType(Type type)
        {
            if (type.IsArray)
            {
                var elementType = FromClrType(type.GetElementType() ?? throw new ArgumentException("Array type must have an element type", nameof(type)));
                return new ArrayAstType(elementType);
            }
            else if (type.IsPointer)
            {
                var elementType = FromClrType(type.GetElementType() ?? throw new ArgumentException("Pointer type must have an element type", nameof(type)));
                return new PointerAstType(elementType);
            }
            else if (type.IsPrimitive || type == typeof(void) || type == typeof(bool))
            {
                return new PrimitiveAstType(type);
            }
            else if (type.IsValueType)
            {
                return new StructAstType(type);
            }
            else
            {
                throw new NotSupportedException($"Unsupported CLR type: {type.FullName}");
            }
        }
    }

    /// <summary>
    /// Represents a primitive type
    /// </summary>
    public class PrimitiveAstType : AstType
    {
        public PrimitiveAstType(Type type) : base(type)
        {
            if (!type.IsPrimitive && type != typeof(void) && type != typeof(bool))
                throw new ArgumentException("Type must be primitive", nameof(type));
        }

        public override string ToOpenClString()
        {
            if (ClrType == typeof(int)) return "int";
            if (ClrType == typeof(uint)) return "uint";
            if (ClrType == typeof(long)) return "long";
            if (ClrType == typeof(ulong)) return "ulong";
            if (ClrType == typeof(short)) return "short";
            if (ClrType == typeof(ushort)) return "ushort";
            if (ClrType == typeof(sbyte)) return "char";
            if (ClrType == typeof(byte)) return "uchar";
            if (ClrType == typeof(Half)) return "half";
            if (ClrType == typeof(float)) return "float";
            if (ClrType == typeof(double)) return "double";
            if (ClrType == typeof(bool)) return "int"; // OpenCL doesn't have bool
            if (ClrType == typeof(void)) return "void";

            return ClrType?.Name ?? "unknown";
        }

        public static readonly PrimitiveAstType Int32 = new(typeof(int));
        public static readonly PrimitiveAstType UInt32 = new(typeof(uint));
        public static readonly PrimitiveAstType Int64 = new(typeof(long));
        public static readonly PrimitiveAstType UInt64 = new(typeof(ulong));
        public static readonly PrimitiveAstType Int16 = new(typeof(short));
        public static readonly PrimitiveAstType UInt16 = new(typeof(ushort));
        public static readonly PrimitiveAstType Int8 = new(typeof(sbyte));
        public static readonly PrimitiveAstType UInt8 = new(typeof(byte));
        public static readonly PrimitiveAstType Float32 = new(typeof(float));
        public static readonly PrimitiveAstType Float64 = new(typeof(double));
        public static readonly PrimitiveAstType Bool = new(typeof(bool));
        public static readonly PrimitiveAstType Boolean = new(typeof(bool));
        public static readonly PrimitiveAstType Void = new(typeof(void));
    }

    /// <summary>
    /// Represents a pointer type
    /// </summary>
    public class PointerAstType : AstType
    {
        public AstType ElementType { get; }
        public override bool IsPointer => true;

        public PointerAstType(AstType elementType) : base(null)
        {
            ElementType = elementType;
        }

        public override string ToOpenClString()
        {
            return $"{ElementType.ToOpenClString()}*";
        }

        public override string ToString()
        {
            return $"{ElementType}*";
        }
    }

    /// <summary>
    /// Represents an array type
    /// </summary>
    public class ArrayAstType : AstType
    {
        public AstType ElementType { get; }
        public override bool IsArray => true;

        public ArrayAstType(AstType elementType) : base(null)
        {
            ElementType = elementType;
        }

        public override string ToOpenClString()
        {
            return $"{ElementType.ToOpenClString()}*"; // Arrays as pointers in OpenCL
        }

        public override string ToString()
        {
            return $"{ElementType}[]";
        }
    }


    /// <summary>
    /// Represents a struct type
    /// </summary>
    public class StructAstType : AstType
    {
        public override bool IsStruct => true;

        public StructAstType(Type clrType) : base(clrType)
        {
            if (!clrType.IsValueType || clrType.IsPrimitive)
                throw new ArgumentException("Type must be a non-primitive struct", nameof(clrType));
        }

        public override string ToOpenClString()
        {
            return ClrType?.Name ?? "UnknownStruct";
        }

        public override string ToString()
        {
            return ClrType?.Name ?? "UnknownStruct";
        }
    }
}