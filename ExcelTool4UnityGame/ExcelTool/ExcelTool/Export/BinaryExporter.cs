using System.Text;
using ExcelTool.Models;

namespace ExcelTool.Export;

/// <summary>
/// 将整张表导出为二进制 .bytes 文件。
///
/// 文件布局（MVP）：
///   Magic     4 bytes  "XTBL"
///   Version   uint16   1
///   RowCount  int32
///   ColCount  byte
///   ColTypes  ColCount × byte（类型 ID）
///   Rows      按列顺序序列化的值
/// </summary>
public static class BinaryExporter
{
    private static readonly byte[] Magic = "XTBL"u8.ToArray();

    public static void Export(TableData table, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);

        writer.Write(Magic);
        writer.Write((ushort)1);
        writer.Write(table.Rows.Count);
        writer.Write((byte)table.Columns.Count);

        foreach (var column in table.Columns)
            writer.Write(BinaryTypeCodec.ToTypeId(column.Type));

        foreach (var row in table.Rows)
        {
            foreach (var column in table.Columns)
            {
                if (!row.TryGetValue(column.Name, out var value))
                    throw new InvalidOperationException(
                        $"导出 [{table.Name}] 时缺少字段 [{column.Name}] 的值。");

                BinaryTypeCodec.WriteValue(writer, column.Type, value);
            }
        }
    }
}
