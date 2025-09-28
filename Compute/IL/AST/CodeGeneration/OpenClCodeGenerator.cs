using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Compute.IL.AST.Expressions;
using Compute.IL.AST.Statements;
using Compute.IL.Utility;
using Mono.Cecil;

namespace Compute.IL.AST.CodeGeneration
{
    /// <summary>
    /// Code generator for OpenCL C language
    /// </summary>
    public class OpenClCodeGenerator : ICodeGenerator, IAstVisitor<string>
    {
        private readonly StringBuilder _builder = new();
        private int _indentLevel = 0;

        public string GenerateBody(IAstNode node)
        {
            _builder.Clear();
            _indentLevel = 0;
            return node.Accept(this);
        }

        public string GenerateType(AstType type)
        {
            return type switch
            {
                PrimitiveAstType primitive => GeneratePrimitiveType(primitive),
                PointerAstType pointer => $"{GenerateType(pointer.ElementType)}*",
                ArrayAstType array => $"{GenerateType(array.ElementType)}*", // Arrays as pointers in OpenCL
                StructAstType structType => $"{GenerateStructName(structType.ClrType!)}",
                _ => throw new NotSupportedException($"Type {type} not supported")
            };
        }

        private object GenerateStructName(Type type)
        {
            var attributes = type.GetCustomAttributes();

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    if (attr is AliasAttribute aliasAttr)
                    {
                        return aliasAttr.Alias;
                    }
                }
            }

            return $"{type.Name}_{type.MetadataToken}";
        }

        public string GenerateFunctionName(AstMethodSource methodSource)
        {
            // Use the same kernel naming convention as the old system
            return $"{methodSource.Method.Name}_method_{methodSource.Method.MetadataToken}";
        }

        public string GenerateFunctionName(MethodReference methodSource)
        {
            var methodBase = TypeHelper.FindMethod(methodSource.Resolve());

            if (methodBase == null)
                throw new InvalidOperationException($"Unable to find method {methodSource.FullName}");

            // Use the same kernel naming convention as the old system
            return $"{methodSource.Name}_method_{methodBase.MetadataToken}";
        }

        private string GeneratePrimitiveType(PrimitiveAstType type)
        {
            if (type.ClrType == typeof(int)) return "int";
            if (type.ClrType == typeof(uint)) return "uint";
            if (type.ClrType == typeof(long)) return "long";
            if (type.ClrType == typeof(ulong)) return "ulong";
            if (type.ClrType == typeof(float)) return "float";
            if (type.ClrType == typeof(double)) return "double";
            if (type.ClrType == typeof(short)) return "short";
            if (type.ClrType == typeof(ushort)) return "ushort";
            if (type.ClrType == typeof(sbyte)) return "char"; // OpenCL
            if (type.ClrType == typeof(byte)) return "uchar"; // OpenCL
            if (type.ClrType == typeof(Half)) return "half";
            if (type.ClrType == typeof(bool)) return "int"; // OpenCL doesn't have bool
            if (type.ClrType == typeof(void)) return "void";

            throw new NotSupportedException($"Primitive type {type.ClrType} not supported");
        }

        private void AppendIndent()
        {
            for (int i = 0; i < _indentLevel; i++)
            {
                _builder.Append("    ");
            }
        }

        public string Visit(IAstNode node)
        {
            return node switch
            {
                // Expressions
                LiteralExpression literal => VisitLiteralExpression(literal),
                IdentifierExpression identifier => VisitIdentifierExpression(identifier),
                BinaryExpression binary => VisitBinaryExpression(binary),
                UnaryExpression unary => VisitUnaryExpression(unary),
                FunctionCallExpression functionCall => VisitFunctionCallExpression(functionCall),
                CastExpression cast => VisitCastExpression(cast),
                ArrayAccessExpression arrayAccess => VisitArrayAccessExpression(arrayAccess),
                FieldAccessExpression fieldAccess => VisitFieldAccessExpression(fieldAccess),
                AddressOfExpression addressOf => VisitAddressOfExpression(addressOf),
                DereferenceExpression dereference => VisitDereferenceExpression(dereference),

                // Statements
                VariableDeclarationStatement varDecl => VisitVariableDeclarationStatement(varDecl),
                AssignmentStatement assignment => VisitAssignmentStatement(assignment),
                ReturnStatement returnStmt => VisitReturnStatement(returnStmt),
                ExpressionStatement exprStmt => VisitExpressionStatement(exprStmt),
                BlockStatement block => VisitBlockStatement(block),
                NopStatement nop => VisitNopStatement(nop),
                LabelStatement label => VisitLabelStatement(label),
                CommentStatement comment => VisitCommentStatement(comment),
                BranchStatement branch => VisitBranchStatement(branch),

                _ => throw new NotSupportedException($"AST node type {node.GetType()} not supported")
            };
        }

        private string VisitLiteralExpression(LiteralExpression literal)
        {
            if (literal.Value is float f)
            {
                var fs = f.ToString();
                if (!fs.Contains('.') && !fs.Contains('E') && !fs.Contains('e')) fs = $"{fs}.0";
                return fs + "f";
            }

            return literal.Value switch
            {
                int i => i.ToString(),
                uint ui => ui.ToString() + "u",
                long l => l.ToString() + "l",
                ulong ul => ul.ToString() + "ul",
                double d => d.ToString("F"),
                bool b => b ? "1" : "0", // OpenCL uses int for bool
                string s => $"\"{s}\"",
                _ => literal.Value?.ToString() ?? "null"
            };
        }

        private string VisitIdentifierExpression(IdentifierExpression identifier)
        {
            return identifier.Name;
        }

        private string VisitBinaryExpression(BinaryExpression binary)
        {
            var left = binary.Left.Accept(this);
            var right = binary.Right.Accept(this);
            var op = BinaryExpression.GetOperatorSymbol(binary.Operator);

            return $"({left} {op} {right})";
        }

        private string VisitUnaryExpression(UnaryExpression unary)
        {
            var operand = unary.Operand.Accept(this);
            var op = UnaryExpression.GetOperatorSymbol(unary.Operator);

            return $"({op}{operand})";
        }

        private string VisitFunctionCallExpression(FunctionCallExpression functionCall)
        {
            var builder = new StringBuilder();

            foreach (var attribute in functionCall.Arguments)
            {
                //var paramType = attribute.Type;

                var arg = attribute.Accept(this);

                /*if (paramType is StructAstType)
                {
                    arg = $"&{arg}";
                }*/

                builder.Append(arg);
                builder.Append(", ");
            }

            if (builder.Length >= 2) builder.Length -= 2; // Remove last ", "

            var methodBase = TypeHelper.FindMethod(functionCall.Method.Resolve());

            if (methodBase == null)
                throw new InvalidOperationException($"Unable to find method {functionCall.Method.FullName}");
            
            var attributes = methodBase.GetCustomAttributes(typeof(AliasAttribute), false);

            if (attributes != null)
            {
                var aliasAttr = attributes.FirstOrDefault() as AliasAttribute;

                if (aliasAttr != null)
                {
                    var aliasName = aliasAttr.Alias;
                    if (aliasName.StartsWith("operator"))
                    {
                        // It's an operator overload
                        var op = aliasName["operator".Length..];
                        if (functionCall.Arguments.Count == 1)
                        {
                            // Unary operator
                            return $"({op}{builder})";
                        }
                        else if (functionCall.Arguments.Count == 2)
                        {
                            // Binary operator
                            var opArgs = builder.ToString().Split([", "], StringSplitOptions.None);
                            return $"({opArgs[0]} {op} {opArgs[1]})";
                        }
                    }
                    else
                    {
                        // It's a regular function with an alias
                        return $"{aliasName}({builder})";
                    }
                }
            }

            var args = builder.Length > 0 ? builder.ToString() : "";

            return $"{GenerateFunctionName(functionCall.Method)}({args})";
        }

        private string VisitCastExpression(CastExpression cast)
        {
            var expr = cast.Expression.Accept(this);
            var type = GenerateType(cast.TargetType);

            return $"(({type}) {expr})";
        }

        private string VisitArrayAccessExpression(ArrayAccessExpression arrayAccess)
        {
            var array = arrayAccess.Array.Accept(this);
            var index = arrayAccess.Index.Accept(this);

            return $"{array}[{index}]";
        }

        private string VisitFieldAccessExpression(FieldAccessExpression fieldAccess)
        {
            var target = fieldAccess.Target.Accept(this);
            var op = fieldAccess.Target.Type.IsPointer ? "->" : ".";

            return $"{target}{op}{fieldAccess.FieldName}";
        }

        private string VisitAddressOfExpression(AddressOfExpression addressOf)
        {
            var expr = addressOf.Expression.Accept(this);
            return $"(&{expr})";
        }

        private string VisitDereferenceExpression(DereferenceExpression dereference)
        {
            var expr = dereference.Expression.Accept(this);

            return $"*{expr}";
        }

        private string VisitVariableDeclarationStatement(VariableDeclarationStatement varDecl)
        {
            var type = GenerateType(varDecl.Type);
            var result = $"{type} {varDecl.Name}";

            if (varDecl.InitialValue != null)
            {
                var value = varDecl.InitialValue.Accept(this);
                result += $" = {value}";
            }

            return result;
        }

        private string VisitAssignmentStatement(AssignmentStatement assignment)
        {
            var target = assignment.Target.Accept(this);
            var value = assignment.Value.Accept(this);

            return $"{target} = {value}";
        }

        private string VisitReturnStatement(ReturnStatement returnStmt)
        {
            if (returnStmt.Value != null)
            {
                var value = returnStmt.Value.Accept(this);
                return $"return {value}";
            }

            return "return";
        }

        private string VisitExpressionStatement(ExpressionStatement exprStmt)
        {
            return exprStmt.Expression.Accept(this);
        }

        private string VisitBlockStatement(BlockStatement block)
        {
            var result = new StringBuilder();

            foreach (var statement in block.Statements)
            {
                var code = statement.Accept(this);
                if (!string.IsNullOrEmpty(code) && !(statement is NopStatement))
                {
                    AppendIndent();
                    result.AppendLine(code + ";");
                }
            }

            return result.ToString();
        }

        private string VisitNopStatement(NopStatement nop)
        {
            return ""; // No-op statements generate no code
        }

        private string VisitLabelStatement(LabelStatement label)
        {
            return $"IL_{label.Offset}:";
        }

        private string VisitCommentStatement(CommentStatement comment)
        {
            return $"// {comment.Comment}";
        }

        private string VisitBranchStatement(BranchStatement branch)
        {
            if (branch.Condition != null)
            {
                var condition = branch.Condition.Accept(this);
                return $"if ({condition}) goto IL_{branch.TargetOffset}";
            }
            else
            {
                return $"goto IL_{branch.TargetOffset}";
            }
        }

        public string GenerateFunctionSignature(AstMethodSource methodSource)
        {
            var builder = new StringBuilder();

            if (methodSource.IsKernel)
            {
                builder.Append("__kernel ");
            }

            // Return type (kernels are always void)
            if (methodSource.IsKernel)
            {
                builder.Append("void ");
            }
            else if (methodSource.Method is MethodInfo mi)
            {
                builder.Append($"{GenerateType(AstType.FromClrType(mi.ReturnType))} ");
            }
            else
            {
                builder.Append("void "); // Constructors, etc.
            }

            // Function name
            builder.Append(GenerateFunctionName(methodSource));

            // Parameters
            builder.Append('(');
            var parameters = methodSource.Method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) builder.Append(", ");

                var attributes = parameters[i].GetCustomAttributes();

                var isByValue = false;

                foreach (var attr in attributes)
                {
                    if (attr is GlobalAttribute)
                    {
                        builder.Append("__global ");
                    }
                    else if (attr is LocalAttribute)
                    {
                        builder.Append("__local ");
                    }
                    else if (attr is ConstantAttribute)
                    {
                        builder.Append("__constant ");
                    }
                    else if (attr is PrivateAttribute)
                    {
                        builder.Append("__private ");
                    }
                    else if (attr is ConstAttribute)
                    {
                        builder.Append("const ");
                    }
                    else if (attr is ByValueAttribute)
                    {
                        isByValue = true;
                    }
                }

                var astType = AstType.FromClrType(parameters[i].ParameterType);

                var type = GenerateType(astType);

                if (astType.IsStruct && !astType.IsPrimitive && !isByValue)
                {
                    type += "*";
                }

                builder.Append($"{type} {parameters[i].Name}");
            }
            builder.Append(')');

            return builder.ToString();
        }

        public string GenerateTypeDefinition(AstType type)
        {
            if (type is StructAstType structType)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"typedef struct {structType.ClrType!.Name}_{structType.ClrType.MetadataToken} {{");

                var fields = structType.ClrType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    var fieldType = GenerateType(AstType.FromClrType(field.FieldType));
                    builder.AppendLine($"    {fieldType} {field.Name};");
                }

                builder.AppendLine($"}} {structType.ClrType.Name}_{structType.ClrType.MetadataToken};");
                return builder.ToString();
            }

            throw new NotSupportedException($"Type definition for {type} not supported");
        }
    }
}