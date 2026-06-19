using System.Text.Json;

namespace ExcelTool.Config;

/// <summary>
/// 从 JSON 文件加载导出配置，并将相对路径解析为绝对路径。
/// </summary>
public static class ExportConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ExportConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"找不到配置文件: {configPath}");

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<ExportConfig>(json, JsonOptions)
                     ?? throw new InvalidOperationException("配置文件内容为空或格式错误。");

        var configDir = Path.GetDirectoryName(Path.GetFullPath(configPath))
                        ?? Directory.GetCurrentDirectory();

        config.ConfigDirectory = configDir;
        config.ExcelRoot = Path.GetFullPath(Path.Combine(configDir, config.ExcelRoot));
        config.CodeOutput = Path.GetFullPath(Path.Combine(configDir, config.CodeOutput));
        config.GlobalOutput = Path.GetFullPath(Path.Combine(configDir, config.GlobalOutput));
        config.DataOutput = Path.GetFullPath(Path.Combine(configDir, config.DataOutput));

        if (!Directory.Exists(config.ExcelRoot))
            throw new DirectoryNotFoundException($"Excel 源目录不存在: {config.ExcelRoot}");

        Directory.CreateDirectory(config.CodeOutput);
        Directory.CreateDirectory(config.GlobalOutput);
        Directory.CreateDirectory(config.DataOutput);

        return config;
    }

    /// <summary>
    /// 按优先级查找配置文件：
    /// 命令行参数 → 当前目录 → 上级目录 → 从 bin 目录回溯到 ExcelTool/。
    /// </summary>
    public static string ResolveConfigPath(string[] args)
    {
        if (args.Length > 0)
            return Path.GetFullPath(args[0]);

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "export-config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "export-config.json"),
            // dotnet run 时 exe 在 ExcelTool/ExcelTool/bin/Debug/net8.0/，向上 4 级到 ExcelTool/
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "export-config.json"),
        };

        foreach (var path in candidates)
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(full))
                return full;
        }

        throw new FileNotFoundException(
            "找不到 export-config.json。请将 Rider 工作目录设为 ExcelTool/，或通过参数传入配置文件路径。");
    }
}
