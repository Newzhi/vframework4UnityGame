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
/// 收集 Myloadtest 结构化结果；默认写入 AB 运行时根目录（与 .bundle 同级）下的 Logs。
/// </summary>
public class LoadApiTestLogCollector : MonoBehaviour
{
    const string DefaultRelativeFolder = "Assets/Test/AB_Test/Logs";
    const string BundleLogSubFolder = "Logs";

    [Header("输出")]
    [Tooltip("写入 BundleResLoader 运行时根目录下的 Logs（与 model.bundle 同目录）")]
    public bool useBundleRootForLogs = true;

    [Tooltip("useBundleRootForLogs=false 时 Editor 用的工程相对路径")]
    public string outputRelativeFolder = DefaultRelativeFolder;

    public bool prettyPrint = true;
    public bool flushOnEachEntry;
    public bool flushOnDestroy = true;

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

    readonly List<LoadApiTestLogEntry> entries = new List<LoadApiTestLogEntry>();
    readonly StringBuilder logViewBuilder = new StringBuilder();
    string sessionStartUtc;
    bool sessionActive;

    public static LoadApiTestLogCollector Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LoadApiTestLog] Duplicate collector; using first instance.");
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
            EndSession();
    }

    void OnDestroy()
    {
        if (flushOnDestroy && sessionActive)
            FlushToFile();

        if (Instance == this)
            Instance = null;
    }

    public bool IsSessionActive => sessionActive;

    public string LastSavedPath => lastSavedPath;

    public void BeginSession(string source = "Myloadtest")
    {
        entries.Clear();
        passCount = 0;
        failCount = 0;
        sessionStartUtc = DateTime.UtcNow.ToString("o");
        sessionId = source + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        sessionActive = true;
        lastSavedPath = null;
        ClearLogView();

        Debug.Log("[LoadApiTestLog] Session started: " + sessionId);
        AppendLine("Session started: " + sessionId);
        AppendLine("Log dir: " + GetOutputDirectoryAbsolute());
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
        if (!sessionActive)
            BeginSession();

        if (passed)
            passCount++;
        else
            failCount++;

        entries.Add(new LoadApiTestLogEntry
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            caseId = caseId,
            roundIndex = roundIndex,
            api = api ?? string.Empty,
            passed = passed,
            detail = detail ?? string.Empty
        });

        string status = passed ? "OK" : "<color=red>FAIL</color>";
        AppendLine(string.Format("Case {0} [{1}] {2} | {3}", caseId, status, api, detail));

        if (flushOnEachEntry)
            FlushToFile();
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
            AppendLine(string.Format("Saved JSON pass={0} fail={1}", passCount, failCount));
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
        if (sessionActive)
        {
            string path = FlushToFile();
            sessionActive = false;
            return path;
        }

        return lastSavedPath;
    }

    public void EndSession()
    {
        SaveSessionAndGetPath();
    }

    LoadApiTestLogSession BuildSession()
    {
        return new LoadApiTestLogSession
        {
            sessionId = sessionId,
            startTimeUtc = sessionStartUtc,
            endTimeUtc = DateTime.UtcNow.ToString("o"),
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            bundleRoot = BundleResLoader.GetDefaultRuntimeBundleRoot(),
            passCount = passCount,
            failCount = failCount,
            entries = entries.ToArray()
        };
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
    public string timestampUtc;
    public int caseId;
    public int roundIndex;
    public string api;
    public bool passed;
    public string detail;
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
    public int passCount;
    public int failCount;
    public LoadApiTestLogEntry[] entries;
}
