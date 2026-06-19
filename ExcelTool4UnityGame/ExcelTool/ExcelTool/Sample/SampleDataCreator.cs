using MiniExcelLibs;

namespace ExcelTool.Sample;

/// <summary>
/// 创建测试用的 Hero.xlsx 样例表（与策划约定的 ## 行格式一致）。
/// 运行方式：dotnet run -- --create-sample
/// </summary>
public static class SampleDataCreator
{
    public static void Create()
    {
        var excelToolDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var testExcelsDir = Path.GetFullPath(Path.Combine(excelToolDir, "..", "TestExcels"));

        Directory.CreateDirectory(testExcelsDir);
        var outputPath = Path.Combine(testExcelsDir, "Hero.xlsx");

        var rows = new List<Dictionary<string, object>>
        {
            new()
            {
                ["A"] = "##var", ["B"] = "id", ["C"] = "name", ["D"] = "hp",
                ["E"] = "level", ["F"] = "enabled", ["G"] = "rewards", ["H"] = "tags",
            },
            new()
            {
                ["A"] = "##type", ["B"] = "uint", ["C"] = "string", ["D"] = "float",
                ["E"] = "int", ["F"] = "bool", ["G"] = "int[]", ["H"] = "string[]",
            },
            new()
            {
                ["A"] = "##Alias", ["B"] = "ID", ["C"] = "名字", ["D"] = "生命值",
                ["E"] = "等级", ["F"] = "启用", ["G"] = "奖励", ["H"] = "标签",
            },
            new()
            {
                ["A"] = "##desc", ["B"] = "唯一主键", ["C"] = "名字", ["D"] = "",
                ["E"] = "", ["F"] = "", ["G"] = "", ["H"] = "",
            },
            new()
            {
                ["A"] = "", ["B"] = "10000", ["C"] = "nick", ["D"] = "100",
                ["E"] = "10", ["F"] = "true", ["G"] = "1|2|3", ["H"] = "atk|def",
            },
            new()
            {
                ["A"] = "", ["B"] = "10001", ["C"] = "leo", ["D"] = "150",
                ["E"] = "20", ["F"] = "1", ["G"] = "10|20", ["H"] = "hp",
            },
            new()
            {
                ["A"] = "", ["B"] = "10002", ["C"] = "ally", ["D"] = "80",
                ["E"] = "5", ["F"] = "false", ["G"] = "", ["H"] = "",
            },
            new()
            {
                ["A"] = "", ["B"] = "10003", ["C"] = "faker", ["D"] = "100",
                ["E"] = "15", ["F"] = "0", ["G"] = "5|6|7|8", ["H"] = "skill|ult",
            },
        };

        MiniExcel.SaveAs(outputPath, rows, overwriteFile: true);
        Console.WriteLine($"已创建样例表: {outputPath}");
    }
}
