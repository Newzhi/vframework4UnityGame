namespace ExcelTool.Models;

/// <summary>
/// Excel ##type 行支持的字段类型（标量 + 数组）。
/// </summary>
public enum FieldType
{
    // 标量
    UInt = 1,
    String = 2,
    Float = 3,
    Int = 4,
    Long = 5,
    Double = 6,
    Bool = 7,

    // 数组（元素用 | 分隔）
    IntArray = 11,
    UIntArray = 12,
    FloatArray = 13,
    StringArray = 14,
    LongArray = 15,
    BoolArray = 16,
}
