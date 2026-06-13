/// <summary>综合测试场景名（与 Scenes 目录下 .unity 文件名一致）。</summary>
public static class ComprehensiveTestPaths
{
    public const string StartSceneName = "StartGame";
    public const string GameSceneName = "PoolTest";

    /// <summary>与 AB_Test 一致：日志子目录名。</summary>
    public const string LogSubFolder = "Logs";

    /// <summary>persistentDataPath 下的根目录（与 AB_Test 的 AB_Test 同级）。</summary>
    public const string PersistentLogRoot = "comprehensiveTest";

    /// <summary>Editor 工程内归档目录（与 LoadApiTestLogCollector 的 DefaultRelativeFolder 同级约定）。</summary>
    public const string EditorRelativeLogFolder = "Assets/Test/comprehensiveTest/Logs";
}
