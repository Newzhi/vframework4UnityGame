using System.Text.RegularExpressions;
using ExcelTool.Models;

namespace ExcelTool.Validate;

/// <summary>
/// 校验表结构与数据合法性，并将字符串行转换为强类型 Rows。
/// </summary>
public static partial class TableValidator
{
    public static List<string> ValidateAndConvert(TableData table)
    {
        var errors = new List<string>();

        ValidateColumns(table, errors);
        if (errors.Count > 0)
            return errors;

        var idColumn = table.Columns[0];
        var seenIds = new HashSet<string>();

        var convertedRows = new List<Dictionary<string, object>>();

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var rawRow = table.Rows[rowIndex];
            var converted = new Dictionary<string, object>();
            var excelRowNumber = rowIndex + 5; // 元数据占 4 行，数据从第 5 行起（用于报错提示）

            foreach (var column in table.Columns)
            {
                if (!rawRow.TryGetValue(column.Name, out var rawValue))
                {
                    errors.Add($"[{table.Name}] 第 {excelRowNumber} 行缺少字段 [{column.Name}]");
                    continue;
                }

                var raw = rawValue?.ToString() ?? "";
                if (!TypeConverter.TryParse(column.Type, raw, out var parsed, out var parseError))
                {
                    errors.Add($"[{table.Name}] 第 {excelRowNumber} 行字段 [{column.Name}]: {parseError}");
                    continue;
                }

                converted[column.Name] = parsed!;
            }

            if (errors.Count > 0)
                continue;

            // 主键唯一：默认第一列 id 不重复
            if (idColumn.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                var idText = converted[idColumn.Name].ToString() ?? "";
                if (!seenIds.Add(idText))
                    errors.Add($"[{table.Name}] 主键 id 重复: {idText}");
            }

            convertedRows.Add(converted);
        }

        if (errors.Count == 0)
            table.Rows.Clear();

        if (errors.Count == 0)
        {
            foreach (var row in convertedRows)
                table.Rows.Add(row);
        }

        return errors;
    }

    private static void ValidateColumns(TableData table, List<string> errors)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in table.Columns)
        {
            if (string.IsNullOrWhiteSpace(column.Name))
            {
                errors.Add($"[{table.Name}] 存在空的字段名。");
                continue;
            }

            if (!FieldNameRegex().IsMatch(column.Name))
                errors.Add($"[{table.Name}] 字段名 [{column.Name}] 不是合法的 C# 标识符。");

            if (!names.Add(column.Name))
                errors.Add($"[{table.Name}] 字段名重复: [{column.Name}]");
        }

        if (table.Rows.Count == 0)
            errors.Add($"[{table.Name}] 没有数据行。");
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex FieldNameRegex();
}
