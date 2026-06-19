namespace ExcelTool.Models;

/// <summary>
/// 一次导出流程的执行结果。
/// </summary>
public sealed class ExportResult
{
    public bool Success { get; init; }
    public List<string> Errors { get; init; } = [];

    public static ExportResult Ok() => new() { Success = true };

    public static ExportResult Fail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };
}
