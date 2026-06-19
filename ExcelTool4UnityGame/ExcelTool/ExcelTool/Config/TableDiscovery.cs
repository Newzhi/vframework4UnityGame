namespace ExcelTool.Config;

/// <summary>
/// 自动发现需要导出的表，避免在配置里手写 growing 的 tables 列表。
/// </summary>
public static class TableDiscovery
{
    public static List<string> Discover(ExportConfig config)
    {
        List<string> tables = config.TableDiscovery.Equals("explicit", StringComparison.OrdinalIgnoreCase)
            ? DiscoverExplicit(config)
            : DiscoverByScan(config);

        if (tables.Count == 0)
            throw new InvalidOperationException($"在 {config.ExcelRoot} 中未找到任何可导出的表。");

        return tables;
    }

    /// <summary>扫描 excelRoot 下所有 .xlsx 文件，文件名即表名。</summary>
    private static List<string> DiscoverByScan(ExportConfig config)
    {
        var exclude = new HashSet<string>(
            config.ExcludeTables ?? [],
            StringComparer.OrdinalIgnoreCase);

        var include = config.IncludeTables is { Count: > 0 }
            ? new HashSet<string>(config.IncludeTables, StringComparer.OrdinalIgnoreCase)
            : null;

        return Directory
            .GetFiles(config.ExcelRoot, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !name!.StartsWith("~$")) // 忽略 Excel 临时锁文件
            .Where(name => !exclude.Contains(name!))
            .Where(name => include is null || include.Contains(name!))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    /// <summary>使用配置中手动指定的 tables 列表（调试 / 局部导表）。</summary>
    private static List<string> DiscoverExplicit(ExportConfig config)
    {
        if (config.Tables is not { Count: > 0 })
            throw new InvalidOperationException("tableDiscovery 为 explicit 时，必须在配置中提供 tables 列表。");

        return config.Tables
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
