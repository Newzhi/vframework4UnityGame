using ExcelTool.Config;
using ExcelTool.Pipeline;
using ExcelTool.Sample;

// Excel 导表工具入口
// 流程：加载 export-config.json → 扫描 Excel → 输出 Metas / Global / Codes

// 开发辅助：生成 TestExcels/Hero.xlsx 样例表
if (args.Length > 0 && args[0] == "--create-sample")
{
    SampleDataCreator.Create();
    return 0;
}

try
{
    var configPath = ExportConfigLoader.ResolveConfigPath(args);
    Console.WriteLine($"配置文件: {configPath}");

    var config = ExportConfigLoader.Load(configPath);
    Console.WriteLine($"Excel 目录:   {config.ExcelRoot}");
    Console.WriteLine($"Meta 输出:    {config.CodeOutput}");
    Console.WriteLine($"Global 输出:  {config.GlobalOutput}");
    Console.WriteLine($"二进制输出:   {config.DataOutput}");
    Console.WriteLine();

    var result = new ExportPipeline().Run(config);

    if (!result.Success)
    {
        Console.Error.WriteLine("导出失败:");
        foreach (var error in result.Errors)
            Console.Error.WriteLine($"  - {error}");
        return 1;
    }

    Console.WriteLine();
    Console.WriteLine("全部导出完成。");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"错误: {ex.Message}");
    return 1;
}
