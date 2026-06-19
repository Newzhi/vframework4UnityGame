using System.Text;
using ExcelTool.Models;

namespace ExcelTool.CodeGen;

/// <summary>
/// 代码生成器共用的命名与类型映射工具。
/// </summary>
internal static class CodeGenHelper
{
    /// <summary>字段名 id → Id，hp → Hp。</summary>
    public static string ToPascalCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name[1..];

    /// <summary>FieldType → 生成 C# 字段类型名。</summary>
    public static string ToCSharpType(FieldType type) =>
        type switch
        {
            FieldType.Int => "int",
            FieldType.UInt => "uint",
            FieldType.Long => "long",
            FieldType.Float => "float",
            FieldType.Double => "double",
            FieldType.Bool => "bool",
            FieldType.String => "string",
            FieldType.IntArray => "int[]",
            FieldType.UIntArray => "uint[]",
            FieldType.LongArray => "long[]",
            FieldType.FloatArray => "float[]",
            FieldType.StringArray => "string[]",
            FieldType.BoolArray => "bool[]",
            _ => "object",
        };

    /// <summary>表类名：Hero → HeroTable。</summary>
    public static string ToTableClassName(string tableName) => $"{tableName}Table";

    /// <summary>Meta 类名：Hero → HeroMeta。</summary>
    public static string ToMetaClassName(string tableName) => $"{tableName}Meta";

    /// <summary>Unity 默认 C# 8：使用块级 namespace，避免 file-scoped namespace（C# 10）。</summary>
    public static void AppendNamespaceOpen(StringBuilder sb, string namespaceName)
    {
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
    }

    /// <summary>与 <see cref="AppendNamespaceOpen"/> 配对闭合。</summary>
    public static void AppendNamespaceClose(StringBuilder sb) => sb.AppendLine("}");
}
