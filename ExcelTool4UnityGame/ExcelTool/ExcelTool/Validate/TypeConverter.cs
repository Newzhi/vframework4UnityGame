using ExcelTool.Models;

namespace ExcelTool.Validate;

/// <summary>
/// 将 Excel 单元格字符串转换为强类型值。
/// 数组单元格用 | 分隔，空单元格表示空数组。
/// </summary>
public static class TypeConverter
{
    private static readonly StringSplitOptions SplitOptions =
        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;

    public static bool TryParse(FieldType type, string raw, out object? value, out string error)
    {
        raw = raw.Trim();
        value = null;
        error = "";

        if (IsArrayType(type))
            return TryParseArray(type, raw, out value, out error);

        switch (type)
        {
            case FieldType.Int:
                if (!int.TryParse(raw, out var i))
                {
                    error = $"无法解析为 int: \"{raw}\"";
                    return false;
                }

                value = i;
                return true;

            case FieldType.UInt:
                if (!uint.TryParse(raw, out var u))
                {
                    error = $"无法解析为 uint: \"{raw}\"";
                    return false;
                }

                value = u;
                return true;

            case FieldType.Long:
                if (!long.TryParse(raw, out var l))
                {
                    error = $"无法解析为 long: \"{raw}\"";
                    return false;
                }

                value = l;
                return true;

            case FieldType.Float:
                if (!float.TryParse(raw, out var f))
                {
                    error = $"无法解析为 float: \"{raw}\"";
                    return false;
                }

                value = f;
                return true;

            case FieldType.Double:
                if (!double.TryParse(raw, out var d))
                {
                    error = $"无法解析为 double: \"{raw}\"";
                    return false;
                }

                value = d;
                return true;

            case FieldType.Bool:
                if (!TryParseBool(raw, out var b))
                {
                    error = $"无法解析为 bool: \"{raw}\"";
                    return false;
                }

                value = b;
                return true;

            case FieldType.String:
                value = raw;
                return true;

            default:
                error = $"未知类型: {type}";
                return false;
        }
    }

    private static bool IsArrayType(FieldType type) =>
        type is FieldType.IntArray or FieldType.UIntArray or FieldType.FloatArray
            or FieldType.StringArray or FieldType.LongArray or FieldType.BoolArray;

    private static bool TryParseArray(FieldType type, string raw, out object? value, out string error)
    {
        value = null;
        error = "";
        var parts = string.IsNullOrWhiteSpace(raw) ? [] : raw.Split('|', SplitOptions);

        switch (type)
        {
            case FieldType.IntArray:
            {
                var arr = new int[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!int.TryParse(parts[i], out arr[i]))
                    {
                        error = $"无法解析为 int[] 第 {i + 1} 项: \"{parts[i]}\"";
                        return false;
                    }
                }

                value = arr;
                return true;
            }
            case FieldType.UIntArray:
            {
                var arr = new uint[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!uint.TryParse(parts[i], out arr[i]))
                    {
                        error = $"无法解析为 uint[] 第 {i + 1} 项: \"{parts[i]}\"";
                        return false;
                    }
                }

                value = arr;
                return true;
            }
            case FieldType.LongArray:
            {
                var arr = new long[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!long.TryParse(parts[i], out arr[i]))
                    {
                        error = $"无法解析为 long[] 第 {i + 1} 项: \"{parts[i]}\"";
                        return false;
                    }
                }

                value = arr;
                return true;
            }
            case FieldType.FloatArray:
            {
                var arr = new float[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!float.TryParse(parts[i], out arr[i]))
                    {
                        error = $"无法解析为 float[] 第 {i + 1} 项: \"{parts[i]}\"";
                        return false;
                    }
                }

                value = arr;
                return true;
            }
            case FieldType.StringArray:
                value = parts;
                return true;
            case FieldType.BoolArray:
            {
                var arr = new bool[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!TryParseBool(parts[i], out arr[i]))
                    {
                        error = $"无法解析为 bool[] 第 {i + 1} 项: \"{parts[i]}\"";
                        return false;
                    }
                }

                value = arr;
                return true;
            }
            default:
                error = $"未知数组类型: {type}";
                return false;
        }
    }

    private static bool TryParseBool(string raw, out bool value)
    {
        if (bool.TryParse(raw, out value))
            return true;

        if (raw == "1")
        {
            value = true;
            return true;
        }

        if (raw == "0")
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }
}
