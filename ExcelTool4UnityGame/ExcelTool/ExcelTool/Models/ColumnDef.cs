namespace ExcelTool.Models;

/// <summary>
/// 一列的元数据，来自 ##var / ##type / ##Alias / ##desc 行。
/// </summary>
public sealed class ColumnDef
{
    /// <summary>字段名（##var），如 id、name。</summary>
    public string Name { get; init; } = "";

    /// <summary>字段类型（##type）。</summary>
    public FieldType Type { get; init; }

    /// <summary>中文别名（##Alias），用于生成 XML 注释。</summary>
    public string Alias { get; init; } = "";

    /// <summary>字段说明（##desc），用于生成 XML 注释。</summary>
    public string Desc { get; init; } = "";
}
