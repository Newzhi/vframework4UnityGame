namespace ExcelTool.Models;

/// <summary>
/// 一张配置表解析后的完整数据，是后续校验、生成代码、导出二进制的中间结构。
/// </summary>
public sealed class TableData
{
    /// <summary>逻辑表名，通常等于 Excel 文件名（不含扩展名）。</summary>
    public string Name { get; init; } = "";

    /// <summary>列定义列表。</summary>
    public List<ColumnDef> Columns { get; init; } = [];

    /// <summary>
    /// 数据行。每行是「字段名 → 已解析的强类型值」。
    /// 校验通过后才会填充。
    /// </summary>
    public List<Dictionary<string, object>> Rows { get; init; } = [];
}
