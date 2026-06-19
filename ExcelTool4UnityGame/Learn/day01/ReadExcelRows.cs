// ============================================================================
// Day01 学习代码：读取 Excel 文件的每一行
//
// 本文件不参与编译，仅供阅读和对照练习。
// 想亲手跑一遍：把 Run() 里的逻辑临时复制到 ExcelTool/ExcelTool/Program.cs，
//               或在 Rider 里对照 TestExcels/Hero.xlsx 阅读即可。
//
// 依赖：MiniExcel（正式工具 ExcelTool 项目已引用）
// API 说明：同目录 MiniExcel.md
// 测试表：TestExcels/Hero.xlsx
// ============================================================================

using MiniExcelLibs;

namespace Learn.Day01;

/// <summary>
/// Day01 演示：用 MiniExcel 按行读取 xlsx，打印每一列（A/B/C/D…）。
/// </summary>
public static class ReadExcelRowsDemo
{
    /// <summary>
    /// 读取并打印 Excel 每一行。
    /// </summary>
    /// <param name="excelPath">xlsx 文件的绝对或相对路径</param>
    public static void Run(string excelPath)
    {
        if (!File.Exists(excelPath))
        {
            Console.WriteLine($"找不到文件: {excelPath}");
            return;
        }

        Console.WriteLine($"读取文件: {Path.GetFullPath(excelPath)}");
        Console.WriteLine(new string('=', 60));

        // -----------------------------------------------------------------
        // 核心：MiniExcel.Query — 详见 Learn/day01/MiniExcel.md
        //
        // 签名：Query(path, useHeaderRow, sheetName, startCell, ...)
        // 返回：IEnumerable<dynamic>，每行可强转为 IDictionary<string, object?>
        // -----------------------------------------------------------------
        var rows = MiniExcel.Query(excelPath, useHeaderRow: false);

        var rowIndex = 0;

        foreach (var row in rows)
        {
            // 每一行 = 「列字母 → 单元格值」
            var cells = (IDictionary<string, object?>)row;

            var excelRowNumber = rowIndex + 1;
            Console.WriteLine($"第 {excelRowNumber} 行 (rowIndex={rowIndex})");
            Console.WriteLine(new string('-', 40));

            if (cells.Count == 0)
            {
                Console.WriteLine("  (空行)");
            }
            else
            {
                // 按 A、B、C… 顺序打印，方便和 Excel 左右对照
                foreach (var col in cells.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    Console.WriteLine($"  列 {col} : {FormatCell(cells[col])}");
                }
            }

            Console.WriteLine();
            rowIndex++;
        }

        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"共 {rowIndex} 行。");
        Console.WriteLine("看 A 列：##var / ##type 是元数据行；A 列为空一般是数据行。");
    }

    /// <summary>单元格值转字符串，方便控制台输出。</summary>
    private static string FormatCell(object? value) =>
        value switch
        {
            null => "(空)",
            string s => string.IsNullOrWhiteSpace(s) ? "(空)" : s,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? "(空)",
        };
}

// ============================================================================
// 调用示例（写在 ExcelTool/Program.cs 里试运行时可参考）：
//
//   var path = Path.GetFullPath("../../TestExcels/Hero.xlsx");
//   Learn.Day01.ReadExcelRowsDemo.Run(path);
//
// 或只取核心三行：
//
//   var rows = MiniExcel.Query(path, useHeaderRow: false);
//   foreach (var row in rows)
//   {
//       var cells = (IDictionary<string, object?>)row;
//       var a = cells.TryGetValue("A", out var v) ? v?.ToString() : "";
//   }
// ============================================================================
