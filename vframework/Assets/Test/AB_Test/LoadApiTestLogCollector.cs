using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 收集 Myloadtest / MyLoadTest2 等并发测试脚本的结构化结果，合并写入同一 JSON。
/// 全局仅允许 <see cref="UnloadAllRunnerSource"/> 调用 UnloadAll（由 Collector 登记）。
/// </summary>
public class LoadApiTestLogCollector : MonoBehaviour
{
    const string DefaultRelativeFolder = "Assets/Test/AB_Test/Logs";
    const string BundleLogSubFolder = "Logs";

    /// <summary>同步套系：允许调用 UnloadAll 的 Runner 标识。</summary>
    public const string UnloadAllRunnerSource = "Myloadtest";

    /// <summary>异步套系：允许调用 UnloadAll 的 Runner 标识。</summary>
    public const string UnloadAllRunnerSourceUni = "MyLoadUniTest";

    [Header("输出")]
    [Tooltip("写入 BundleResLoader 运行时根目录下的 Logs（与 model.bundle 同目录）")]
    public bool useBundleRootForLogs = true;

    [Tooltip("useBundleRootForLogs=false 时 Editor 用的工程相对路径")]
    public string outputRelativeFolder = DefaultRelativeFolder;

    public bool prettyPrint = true;
    public bool flushOnEachEntry;
    public bool flushOnDestroy = true;

    [Header("并发会话")]
    [Tooltip("几个测试脚本同时跑时设为相同数量；全部 NotifyRunnerComplete 后写入同一文件")]
    public int expectedConcurrentRunners = 2;

    [Tooltip("JSON sessionId 前缀；同步套系 ConcurrentLoad_，异步套系 UniConcurrentLoad_")]
    public string sessionIdPrefix = "ConcurrentLoad_";

    [Tooltip("允许调用 UnloadAll 的 Runner；同步套系 Myloadtest，异步套系 MyLoadUniTest")]
    public string unloadAllRunnerSource = UnloadAllRunnerSource;

    /// <summary>当前会话配置的 UnloadAll 独占 Runner。</summary>
    public string AllowedUnloadAllRunner => unloadAllRunnerSource;

    [Header("引用计数")]
    [Tooltip("每条 Record 自动附带 Resource/Bundle 层 Ref 快照")]
    public bool appendRefSnapshotOnRecord = true;

    [Header("UI")]
    [Tooltip("场景 ShowLog 等 Text，实时显示测试日志")]
    public Text logText;

    [Tooltip("UI 最多保留行数，超出从顶部删除")]
    public int maxDisplayLines = 150;

    [Header("会话（只读）")]
    [SerializeField] string sessionId;
    [SerializeField] int passCount;
    [SerializeField] int failCount;
    [SerializeField] string lastSavedPath;
    [SerializeField] string[] registeredRunnersSnapshot;
    [SerializeField] string[] completedRunnersSnapshot;
    [SerializeField] string unloadAllRunnerClaimed;

    readonly List<LoadApiTestLogEntry> entries = new List<LoadApiTestLogEntry>();
    readonly HashSet<string> registeredRunners = new HashSet<string>();
    readonly HashSet<string> completedRunners = new HashSet<string>();
    readonly StringBuilder logViewBuilder = new StringBuilder();
    string sessionStartUtc;
    bool sessionActive;
    int entrySequence;

    public static LoadApiTestLogCollector Instance { get; private set; }

    /// <summary>多个测试脚本应指向同一 Collector；返回全局实例或本地引用。</summary>
    public static LoadApiTestLogCollector EnsureShared(LoadApiTestLogCollector local)
    {
        if (Instance != null)
            return Instance;

        return local;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LoadApiTestLog] Duplicate collector; tests should share one LoadApiTestLogCollector.");
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        Application.quitting += OnApplicationQuitting;
    }

    void OnDisable()
    {
        Application.quitting -= OnApplicationQuitting;
    }

    void OnApplicationQuitting()
    {
        if (sessionActive)
            ForceEndSession();
    }

    void OnDestroy()
    {
        if (flushOnDestroy && sessionActive)
            ForceEndSession();

        if (Instance == this)
            Instance = null;
    }

    public bool IsSessionActive => sessionActive;

    public string LastSavedPath => lastSavedPath;

    /// <summary>指定 Runner 是否已登记参与本会话。</summary>
    public bool IsRunnerRegistered(string source)
    {
        source = NormalizeSource(source);
        return registeredRunners.Contains(source);
    }

    /// <summary>指定 Runner 是否已跑完并 NotifyRunnerComplete。</summary>
    public bool IsRunnerComplete(string source)
    {
        source = NormalizeSource(source);
        return completedRunners.Contains(source);
    }

    /// <summary>
    /// 登记 UnloadAll 调用方。仅 <see cref="UnloadAllRunnerSource"/> 可成功；其它来源返回 false。
    /// </summary>
    public bool TryClaimUnloadAll(string source)
    {
        source = NormalizeSource(source);

        if (source != unloadAllRunnerSource)
        {
            AppendLine(string.Format(
                "<color=red>[UnloadAll] denied for {0}; only {1} may call UnloadAll</color>",
                source,
                unloadAllRunnerSource));
            return false;
        }

        if (!string.IsNullOrEmpty(unloadAllRunnerClaimed))
            return unloadAllRunnerClaimed == source;

        unloadAllRunnerClaimed = source;
        AppendLine("[UnloadAll] claimed by " + source);
        return true;
    }

    public bool HasUnloadAllBeenClaimed => !string.IsNullOrEmpty(unloadAllRunnerClaimed);

    /// <summary>
    /// 首个 Runner 创建共享会话；后续 Runner 只登记来源，不清空已有日志。
    /// </summary>
    public void BeginSession(string source = "Myloadtest")
    {
        source = NormalizeSource(source);

        if (!sessionActive)
        {
            entries.Clear();
            registeredRunners.Clear();
            completedRunners.Clear();
            passCount = 0;
            failCount = 0;
            entrySequence = 0;
            sessionStartUtc = DateTime.UtcNow.ToString("o");
            sessionId = BuildSessionId(source, sessionIdPrefix);
            sessionActive = true;
            lastSavedPath = null;
            unloadAllRunnerClaimed = null;
            ClearLogView();

            Debug.Log("[LoadApiTestLog] Shared session started: " + sessionId);
            AppendLine("Session started: " + sessionId);
            AppendLine("Log dir: " + GetOutputDirectoryAbsolute());
            AppendLine("Expected runners: " + expectedConcurrentRunners);
        }

        if (registeredRunners.Add(source))
            AppendLine("Runner joined: " + source);

        SyncRunnerSnapshots();
    }

    public void ClearLogView()
    {
        logViewBuilder.Clear();
        if (logText != null)
            logText.text = string.Empty;
    }

    public void AppendLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        if (logViewBuilder.Length > 0)
            logViewBuilder.AppendLine();
        logViewBuilder.Append(line);
        TrimLogViewLinesIfNeeded();

        if (logText != null)
            logText.text = logViewBuilder.ToString();
    }

    public void Record(int caseId, int roundIndex, string api, bool passed, string detail)
    {
        Record("Unknown", caseId, roundIndex, api, passed, detail);
    }

    /// <summary>带来源标识的记录，便于两个脚本并发时区分。</summary>
    public void Record(string source, int caseId, int roundIndex, string api, bool passed, string detail)
    {
        source = NormalizeSource(source);

        if (!sessionActive)
            BeginSession(source);
        else if (!registeredRunners.Contains(source))
            BeginSession(source);

        if (passed)
            passCount++;
        else
            failCount++;

        entrySequence++;
        string refSnapshot = appendRefSnapshotOnRecord ? CaptureReferenceSnapshot() : null;
        entries.Add(new LoadApiTestLogEntry
        {
            sequence = entrySequence,
            timestampUtc = DateTime.UtcNow.ToString("o"),
            source = source,
            caseId = caseId,
            roundIndex = roundIndex,
            api = api ?? string.Empty,
            passed = passed,
            detail = detail ?? string.Empty,
            refSnapshot = refSnapshot ?? string.Empty
        });

        string status = passed ? "OK" : "<color=red>FAIL</color>";
        AppendLine(string.Format("[{0}] Case {1} [{2}] {3} | {4}", source, caseId, status, api, detail));
        if (appendRefSnapshotOnRecord && !string.IsNullOrEmpty(refSnapshot))
            AppendLine("  Ref " + refSnapshot);

        if (flushOnEachEntry)
            FlushToFile();
    }

    /// <summary>手动打一条 Ref 快照到 UI / JSON（不写 pass/fail）。</summary>
    [ContextMenu("Append Ref Snapshot")]
    public void AppendReferenceSnapshot(string source = "Manual")
    {
        if (!sessionActive)
            BeginSession(source);

        string snapshot = CaptureReferenceSnapshot();
        AppendLine("[" + NormalizeSource(source) + "] Ref snapshot: " + snapshot);
        Debug.Log("[LoadApiTestLog] Ref snapshot: " + snapshot);
    }

    static string CaptureReferenceSnapshot()
    {
        // try
        // {
        //     //return BundleResLoader.Instance.BuildReferenceSnapshot();
        // }
        // catch (Exception ex)
        // {
        //     //return "snapshot_error:" + ex.Message;
        // }
        return null;
    }

    /// <summary>单个 Runner 跑完；全部到齐后自动写入同一 JSON。</summary>
    public void NotifyRunnerComplete(string source)
    {
        if (!sessionActive)
            return;

        source = NormalizeSource(source);
        if (!registeredRunners.Contains(source))
            registeredRunners.Add(source);

        if (!completedRunners.Add(source))
            return;

        AppendLine(string.Format(
            "Runner finished: {0} ({1}/{2})",
            source,
            completedRunners.Count,
            Math.Max(expectedConcurrentRunners, registeredRunners.Count)));

        SyncRunnerSnapshots();

        if (completedRunners.Count >= expectedConcurrentRunners)
            SaveSessionAndGetPath();
    }

    [ContextMenu("Flush To JSON")]
    public string FlushToFile()
    {
        if (!sessionActive)
        {
            Debug.LogWarning("[LoadApiTestLog] No active session to flush.");
            return lastSavedPath;
        }

        EnsureOutputDirectory();

        string fileName = sessionId + ".json";
        string fullPath = Path.Combine(GetOutputDirectoryAbsolute(), fileName);

        try
        {
            LoadApiTestLogSession session = BuildSession();
            string json = JsonUtility.ToJson(session, prettyPrint);
            File.WriteAllText(fullPath, json);

            lastSavedPath = fullPath;
            Debug.Log("[LoadApiTestLog] Saved: " + fullPath);
            AppendLine(string.Format("Saved JSON pass={0} fail={1} entries={2}", passCount, failCount, entries.Count));
            AppendLine(fullPath);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            return fullPath;
        }
        catch (Exception ex)
        {
            string message = "Save failed: " + ex.Message;
            Debug.LogError("[LoadApiTestLog] " + message);
            AppendLine("<color=red>" + message + "</color>");
            return null;
        }
    }

    public string SaveSessionAndGetPath()
    {
        if (!sessionActive)
            return lastSavedPath;

        string path = FlushToFile();
        sessionActive = false;
        return path;
    }

    public void EndSession()
    {
        ForceEndSession();
    }

    /// <summary>立即落盘（例如 Exit 按钮），不等待其它 Runner。</summary>
    public void ForceEndSession()
    {
        if (!sessionActive)
            return;

        AppendLine("Force end session");
        SaveSessionAndGetPath();
    }

    LoadApiTestLogSession BuildSession()
    {
        LoadApiTestLogEntry[] sorted = entries.ToArray();
        Array.Sort(sorted, CompareEntries);

        return new LoadApiTestLogSession
        {
            sessionId = sessionId,
            startTimeUtc = sessionStartUtc,
            endTimeUtc = DateTime.UtcNow.ToString("o"),
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            bundleRoot = BundleResLoader.GetDefaultRuntimeBundleRoot(),
            expectedConcurrentRunners = expectedConcurrentRunners,
            registeredRunners = SnapshotSet(registeredRunners),
            completedRunners = SnapshotSet(completedRunners),
            unloadAllRunner = unloadAllRunnerClaimed ?? string.Empty,
            passCount = passCount,
            failCount = failCount,
            entries = sorted
        };
    }

    static int CompareEntries(LoadApiTestLogEntry a, LoadApiTestLogEntry b)
    {
        int bySequence = a.sequence.CompareTo(b.sequence);
        if (bySequence != 0)
            return bySequence;

        return string.CompareOrdinal(a.timestampUtc, b.timestampUtc);
    }

    static string BuildSessionId(string firstSource, string prefix)
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        if (string.IsNullOrEmpty(prefix))
            prefix = "ConcurrentLoad_";

        return prefix + firstSource + "_" + stamp;
    }

    static string NormalizeSource(string source)
    {
        return string.IsNullOrEmpty(source) ? "Unknown" : source;
    }

    static string[] SnapshotSet(HashSet<string> set)
    {
        string[] arr = new string[set.Count];
        set.CopyTo(arr);
        Array.Sort(arr, StringComparer.Ordinal);
        return arr;
    }

    void SyncRunnerSnapshots()
    {
        registeredRunnersSnapshot = SnapshotSet(registeredRunners);
        completedRunnersSnapshot = SnapshotSet(completedRunners);
    }

    void EnsureOutputDirectory()
    {
        string dir = GetOutputDirectoryAbsolute();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    string GetOutputDirectoryAbsolute()
    {
        if (useBundleRootForLogs)
        {
            string bundleRoot = BundleResLoader.GetDefaultRuntimeBundleRoot();
            string preferred = Path.Combine(bundleRoot, BundleLogSubFolder);
            if (TryEnsureWritableDirectory(preferred, out string writableDir))
                return writableDir;

            string fallback = Path.Combine(Application.persistentDataPath, "AB_Test", BundleLogSubFolder);
            Debug.LogWarning("[LoadApiTestLog] Bundle log dir not writable, fallback: " + fallback);
            return fallback;
        }

#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string relative = string.IsNullOrEmpty(outputRelativeFolder)
            ? DefaultRelativeFolder
            : outputRelativeFolder.Replace("\\", "/");
        return Path.GetFullPath(Path.Combine(projectRoot, relative));
#else
        return Path.Combine(Application.persistentDataPath, "AB_Test", BundleLogSubFolder);
#endif
    }

    static bool TryEnsureWritableDirectory(string dir, out string writableDir)
    {
        writableDir = dir;
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string probe = Path.Combine(dir, ".write_probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    void TrimLogViewLinesIfNeeded()
    {
        if (maxDisplayLines <= 0)
            return;

        string text = logViewBuilder.ToString();
        int lineCount = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                lineCount++;
        }

        if (lineCount <= maxDisplayLines)
            return;

        int linesToRemove = lineCount - maxDisplayLines;
        int start = 0;
        for (int i = 0; i < text.Length && linesToRemove > 0; i++)
        {
            if (text[i] != '\n')
                continue;

            linesToRemove--;
            start = i + 1;
        }

        logViewBuilder.Clear();
        if (start < text.Length)
            logViewBuilder.Append(text, start, text.Length - start);
    }
}

[Serializable]
public class LoadApiTestLogEntry
{
    public int sequence;
    public string timestampUtc;
    public string source;
    public int caseId;
    public int roundIndex;
    public string api;
    public bool passed;
    public string detail;
    public string refSnapshot;
}

[Serializable]
public class LoadApiTestLogSession
{
    public string sessionId;
    public string startTimeUtc;
    public string endTimeUtc;
    public string unityVersion;
    public string platform;
    public string bundleRoot;
    public int expectedConcurrentRunners;
    public string[] registeredRunners;
    public string[] completedRunners;
    public string unloadAllRunner;
    public int passCount;
    public int failCount;
    public LoadApiTestLogEntry[] entries;
}
