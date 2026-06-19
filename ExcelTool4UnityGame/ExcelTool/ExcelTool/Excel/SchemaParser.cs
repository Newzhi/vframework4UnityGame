using ExcelTool.Models;

namespace ExcelTool.Excel;

/// <summary>
/// 解析 Excel 元数据行（##var / ##type / ##Alias / ##desc）和数据行。
/// A 列为行标记，B 列起才是字段内容。
/// </summary>
public static class SchemaParser
{
    private const string VarTag = "##var";
    private const string TypeTag = "##type";
    private const string AliasTag = "##Alias";
    private const string DescTag = "##desc";

    public static TableData Parse(List<Dictionary<string, object?>> rawRows, string tableName)
    {
        var varRow = FindTaggedRow(rawRows, VarTag)
                     ?? throw new InvalidOperationException($"表 [{tableName}] 缺少 {VarTag} 行。");
        var typeRow = FindTaggedRow(rawRows, TypeTag)
                      ?? throw new InvalidOperationException($"表 [{tableName}] 缺少 {TypeTag} 行。");
        var aliasRow = FindTaggedRow(rawRows, AliasTag);
        var descRow = FindTaggedRow(rawRows, DescTag);

        var fieldNames = ReadCellsFromColumnB(varRow.Row);
        var fieldTypes = ReadCellsFromColumnB(typeRow.Row);

        if (fieldNames.Count == 0)
            throw new InvalidOperationException($"表 [{tableName}] 的 {VarTag} 行没有定义任何字段。");

        if (fieldNames.Count != fieldTypes.Count)
            throw new InvalidOperationException(
                $"表 [{tableName}] 的 {VarTag} 与 {TypeTag} 列数不一致（{fieldNames.Count} vs {fieldTypes.Count}）。");

        var aliases = aliasRow is not null ? ReadCellsFromColumnB(aliasRow.Row) : [];
        var descs = descRow is not null ? ReadCellsFromColumnB(descRow.Row) : [];

        var columns = new List<ColumnDef>();
        for (var i = 0; i < fieldNames.Count; i++)
        {
            columns.Add(new ColumnDef
            {
                Name = fieldNames[i],
                Type = ParseFieldType(fieldTypes[i], tableName, fieldNames[i]),
                Alias = i < aliases.Count ? aliases[i] : "",
                Desc = i < descs.Count ? descs[i] : "",
            });
        }

        var table = new TableData
        {
            Name = tableName,
            Columns = columns,
        };

        // 数据区：##desc 的下一行开始；若没有 ##desc 则从 ##type 下一行开始
        var dataStartIndex = (descRow ?? typeRow).RowIndex + 1;
        for (var r = dataStartIndex; r < rawRows.Count; r++)
        {
            var row = rawRows[r];
            var tag = GetCell(row, "A").Trim();

            // 跳过空行和重复的元数据行
            if (tag.StartsWith("##", StringComparison.Ordinal))
                continue;

            var values = ReadDataCells(row, columns.Count);
            if (values.All(string.IsNullOrWhiteSpace))
                continue;

            var rowDict = new Dictionary<string, string>();
            for (var c = 0; c < columns.Count; c++)
                rowDict[columns[c].Name] = values[c];

            table.Rows.Add(rowDict.ToDictionary(kv => kv.Key, kv => (object)kv.Value));
        }

        return table;
    }

    private static TaggedRow? FindTaggedRow(List<Dictionary<string, object?>> rows, string tag)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (GetCell(rows[i], "A").Equals(tag, StringComparison.OrdinalIgnoreCase))
                return new TaggedRow(i, rows[i]);
        }

        return null;
    }

    /// <summary>从 B 列起依次读取单元格文本（跳过 A 列的行标记）。</summary>
    private static List<string> ReadCellsFromColumnB(Dictionary<string, object?> row)
    {
        var result = new List<string>();
        for (var col = 2; ; col++)
        {
            var key = ColumnIndexToLetter(col);
            if (!row.ContainsKey(key))
                break;

            var text = GetCell(row, key);
            result.Add(text);
        }

        // 去掉末尾连续空列
        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
            result.RemoveAt(result.Count - 1);

        return result;
    }

    private static List<string> ReadDataCells(Dictionary<string, object?> row, int columnCount)
    {
        var result = new List<string>(columnCount);
        for (var i = 0; i < columnCount; i++)
        {
            var key = ColumnIndexToLetter(i + 2); // B=2
            result.Add(row.TryGetValue(key, out var value) ? CellToString(value) : "");
        }

        return result;
    }

    private static FieldType ParseFieldType(string typeText, string tableName, string fieldName)
    {
        return typeText.Trim().ToLowerInvariant() switch
        {
            "int" => FieldType.Int,
            "uint" => FieldType.UInt,
            "long" => FieldType.Long,
            "float" => FieldType.Float,
            "double" => FieldType.Double,
            "bool" => FieldType.Bool,
            "string" => FieldType.String,
            "int[]" => FieldType.IntArray,
            "uint[]" => FieldType.UIntArray,
            "float[]" => FieldType.FloatArray,
            "string[]" => FieldType.StringArray,
            "long[]" => FieldType.LongArray,
            "bool[]" => FieldType.BoolArray,
            _ => throw new InvalidOperationException(
                $"表 [{tableName}] 字段 [{fieldName}] 使用了不支持的类型: {typeText}"),
        };
    }

    private static string GetCell(Dictionary<string, object?> row, string columnLetter) =>
        row.TryGetValue(columnLetter, out var value) ? CellToString(value) : "";

    private static string CellToString(object? value) =>
        value switch
        {
            null => "",
            string s => s.Trim(),
            DateTime dt => dt.ToString("O"),
            bool b => b ? "true" : "false",
            _ => value.ToString()?.Trim() ?? "",
        };

    /// <summary>列序号转 Excel 列字母：1→A, 2→B, 27→AA。</summary>
    private static string ColumnIndexToLetter(int index)
    {
        var letters = "";
        while (index > 0)
        {
            var rem = (index - 1) % 26;
            letters = (char)('A' + rem) + letters;
            index = (index - 1) / 26;
        }

        return letters;
    }

    private sealed record TaggedRow(int RowIndex, Dictionary<string, object?> Row);
}
