using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 收集 LoadApiTester 结构化结果，Play 结束时写入 JSON（Editor 下默认 Assets/Test/AB_Test/Logs）。
/// </summary>
public class LoadApiTestLogCollector : MonoBehaviour
{
    const string DefaultRelativeFolder = "Assets/Test/AB_Test/Logs";

    [Header("输出")]
    [Tooltip("相对工程根目录；Editor Play 下写入该路径")]
    public string outputRelativeFolder = DefaultRelativeFolder;

    public bool prettyPrint = true;
    public bool flushOnEachEntry;
    public bool flushOnDestroy = true;

    [Header("会话（只读）")]
    [SerializeField] string sessionId;
    [SerializeField] int passCount;
    [SerializeField] int failCount;
    [SerializeField] string lastSavedPath;

    readonly List<LoadApiTestLogEntry> entries = new List<LoadApiTestLogEntry>();
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

    public void BeginSession(string source = "LoadApiTester")
    {
        entries.Clear();
        passCount = 0;
        failCount = 0;
        sessionStartUtc = DateTime.UtcNow.ToString("o");
        sessionId = source + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        sessionActive = true;
        lastSavedPath = null;

        Debug.Log("[LoadApiTestLog] Session started: " + sessionId);
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

        if (flushOnEachEntry)
            FlushToFile();
    }

    [ContextMenu("Flush To JSON")]
    public string FlushToFile()
    {
        if (!sessionActive)
        {
            Debug.LogWarning("[LoadApiTestLog] No active session to flush.");
            return null;
        }

        EnsureOutputDirectory();

        string fileName = sessionId + ".json";
        string fullPath = Path.Combine(GetOutputDirectoryAbsolute(), fileName);

        LoadApiTestLogSession session = BuildSession();
        string json = JsonUtility.ToJson(session, prettyPrint);
        File.WriteAllText(fullPath, json);

        lastSavedPath = fullPath;
        Debug.Log("[LoadApiTestLog] Saved: " + fullPath);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        return fullPath;
    }

    public void EndSession()
    {
        if (!sessionActive)
            return;

        FlushToFile();
        sessionActive = false;
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
#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string relative = string.IsNullOrEmpty(outputRelativeFolder)
            ? DefaultRelativeFolder
            : outputRelativeFolder.Replace("\\", "/");
        return Path.GetFullPath(Path.Combine(projectRoot, relative));
#else
        return Path.Combine(Application.persistentDataPath, "AB_Test", "Logs");
#endif
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
