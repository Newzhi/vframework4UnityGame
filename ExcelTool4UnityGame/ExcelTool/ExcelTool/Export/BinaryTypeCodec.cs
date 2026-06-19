using System.Text;
using ExcelTool.Models;

namespace ExcelTool.Export;

/// <summary>
/// 将单个字段值按约定格式写入二进制流。
/// 数组格式：int32 长度 + 按序写入每个元素。
/// </summary>
public static class BinaryTypeCodec
{
    public static void WriteValue(BinaryWriter writer, FieldType type, object value)
    {
        switch (type)
        {
            case FieldType.Int:
                writer.Write(Convert.ToInt32(value));
                break;

            case FieldType.UInt:
                writer.Write(Convert.ToUInt32(value));
                break;

            case FieldType.Long:
                writer.Write(Convert.ToInt64(value));
                break;

            case FieldType.Float:
                writer.Write(Convert.ToSingle(value));
                break;

            case FieldType.Double:
                writer.Write(Convert.ToDouble(value));
                break;

            case FieldType.Bool:
                writer.Write(Convert.ToBoolean(value));
                break;

            case FieldType.String:
                WriteString(writer, value?.ToString() ?? "");
                break;

            case FieldType.IntArray:
                WriteIntArray(writer, (int[])value);
                break;

            case FieldType.UIntArray:
                WriteUIntArray(writer, (uint[])value);
                break;

            case FieldType.LongArray:
                WriteLongArray(writer, (long[])value);
                break;

            case FieldType.FloatArray:
                WriteFloatArray(writer, (float[])value);
                break;

            case FieldType.StringArray:
                WriteStringArray(writer, (string[])value);
                break;

            case FieldType.BoolArray:
                WriteBoolArray(writer, (bool[])value);
                break;

            default:
                throw new NotSupportedException($"不支持的类型: {type}");
        }
    }

    public static byte ToTypeId(FieldType type) =>
        type switch
        {
            FieldType.UInt => 1,
            FieldType.String => 2,
            FieldType.Float => 3,
            FieldType.Int => 4,
            FieldType.Long => 5,
            FieldType.Double => 6,
            FieldType.Bool => 7,
            FieldType.IntArray => 11,
            FieldType.UIntArray => 12,
            FieldType.FloatArray => 13,
            FieldType.StringArray => 14,
            FieldType.LongArray => 15,
            FieldType.BoolArray => 16,
            _ => throw new NotSupportedException($"不支持的类型: {type}"),
        };

    private static void WriteString(BinaryWriter writer, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteIntArray(BinaryWriter writer, int[] arr)
    {
        writer.Write(arr.Length);
        foreach (var n in arr)
            writer.Write(n);
    }

    private static void WriteUIntArray(BinaryWriter writer, uint[] arr)
    {
        writer.Write(arr.Length);
        foreach (var n in arr)
            writer.Write(n);
    }

    private static void WriteLongArray(BinaryWriter writer, long[] arr)
    {
        writer.Write(arr.Length);
        foreach (var n in arr)
            writer.Write(n);
    }

    private static void WriteFloatArray(BinaryWriter writer, float[] arr)
    {
        writer.Write(arr.Length);
        foreach (var n in arr)
            writer.Write(n);
    }

    private static void WriteStringArray(BinaryWriter writer, string[] arr)
    {
        writer.Write(arr.Length);
        foreach (var s in arr)
            WriteString(writer, s);
    }

    private static void WriteBoolArray(BinaryWriter writer, bool[] arr)
    {
        writer.Write(arr.Length);
        foreach (var b in arr)
            writer.Write(b);
    }
}
