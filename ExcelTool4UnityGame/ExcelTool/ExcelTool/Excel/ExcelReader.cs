using MiniExcelLibs;

namespace ExcelTool.Excel;

/// <summary>
/// 使用 MiniExcel 读取 xlsx，返回按列字母索引（A、B、C…）的原始行数据。
/// 不做业务解析，只负责 IO。
/// </summary>
public static class ExcelReader
{
    /// <summary>
    /// 读取 Excel 所有行。useHeaderRow=false 表示不把第一行当作表头，
    /// 这样我们可以按固定行号解析 ##var 等元数据行。
    /// </summary>
    public static List<Dictionary<string, object?>> ReadRows(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"找不到 Excel 文件: {filePath}");

        return MiniExcel.Query(filePath, useHeaderRow: false)
            .Cast<IDictionary<string, object?>>()
            .Select(row => row.ToDictionary(kv => kv.Key, kv => kv.Value))
            .ToList();
    }
}
