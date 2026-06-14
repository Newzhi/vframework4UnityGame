using System;
using System.Collections.Generic;
using System.Text;
using BaseFramework.BaseEventSys;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

/// <summary>
/// 综合测试专用日志：事件总线、对象池快照、内存趋势；绑定 UI Text 与退出按钮。
/// 重要事件（死亡/复活/系统/池快照）优先保留，支持长时间对局导出。
/// </summary>
public class ComprehensiveTestLogger : MonoBehaviour
{
    const string BulletPath = "Model/Prefabs/Bullet";
    const string EnemyPath = "Model/Prefabs/tester";
    const float DamageLogInterval = 1f;
    const float SnapshotInterval = 5f;
    const int LeakTrendSamples = 6;
    const long LeakMonoGrowthThreshold = 5 * 1024 * 1024;
    const int DefaultMaxStoredLines = 12000;

    [SerializeField] Text logText;
    [SerializeField] Button exitButton;
    [SerializeField] bool logToConsole = true;
    [SerializeField] bool logMemory = true;
    [SerializeField] bool logShots = false;
    [SerializeField] int maxUiLines = 18;
    [SerializeField] int maxStoredLines = DefaultMaxStoredLines;

    struct LogEntry
    {
        public string Line;
        public bool Pin;
    }

    readonly List<LogEntry> entries = new List<LogEntry>();
    readonly List<long> monoUsedHistory = new List<long>();

    float nextDamageLogTime;
    float nextSnapshotTime;
    long baselineMonoUsed;
    long baselineTotalReserved;
    long baselineGcManaged;

    void Awake()
    {
        if (logText == null)
            logText = GameObject.Find("Log")?.GetComponent<Text>();

        if (exitButton == null)
            exitButton = GameObject.Find("ExitGame")?.GetComponent<Button>();

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitGame);
    }

    void OnDestroy()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitGame);
    }

    void OnEnable()
    {
        GameEventBus.RegisterEvent<PlayerShotEvent>(OnPlayerShot);
        GameEventBus.RegisterEvent<EnemyShotEvent>(OnEnemyShot);
        GameEventBus.RegisterEvent<DamageTakenEvent>(OnDamageTaken);
        GameEventBus.RegisterEvent<EntityDeadEvent>(OnEntityDead);
        GameEventBus.RegisterEvent<EnemySpawnedEvent>(OnEnemySpawned);
        GameEventBus.RegisterEvent<PlayerRespawnedEvent>(OnPlayerRespawned);
    }

    void OnDisable()
    {
        GameEventBus.DeRegisterEvent<PlayerShotEvent>(OnPlayerShot);
        GameEventBus.DeRegisterEvent<EnemyShotEvent>(OnEnemyShot);
        GameEventBus.DeRegisterEvent<DamageTakenEvent>(OnDamageTaken);
        GameEventBus.DeRegisterEvent<EntityDeadEvent>(OnEntityDead);
        GameEventBus.DeRegisterEvent<EnemySpawnedEvent>(OnEnemySpawned);
        GameEventBus.DeRegisterEvent<PlayerRespawnedEvent>(OnPlayerRespawned);
    }

    void Start()
    {
        if (maxStoredLines < 2000)
            maxStoredLines = DefaultMaxStoredLines;

        if (logText != null)
        {
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        Write("Logger启动", pin: true);
        Write("导出:" + ComprehensiveTestLogExporter.GetExportDirectory(), pin: true);
        Write("真机:" + ComprehensiveTestLogExporter.GetPersistentLogDirectory(), pin: true);
        Write(ComprehensiveTestLogExporter.GetLocationHint(), pin: true);

        bool bundleReady = BundleResLoader.Instance.EnsureReady();
        Write(bundleReady ? "EnsureReady OK" : "EnsureReady FAIL", pin: true, isError: !bundleReady);

        CaptureMemoryBaseline();
        if (logMemory)
            LogMemorySnapshot("基线", pin: true);

        nextSnapshotTime = Time.time + 2f;
    }

    void Update()
    {
        if (Time.time < nextSnapshotTime)
            return;

        nextSnapshotTime = Time.time + SnapshotInterval;
        LogPoolSnapshot();
        if (logMemory)
            LogMemorySnapshot("周期");
    }

    void OnPlayerShot(PlayerShotEvent e)
    {
        if (!logShots)
            return;

        Write("PS " + Fv(e.Position));
    }

    void OnEnemyShot(EnemyShotEvent e)
    {
        if (!logShots)
            return;

        Write("ES " + Fv(e.Position));
    }

    void OnDamageTaken(DamageTakenEvent e)
    {
        if (Time.time < nextDamageLogTime)
            return;

        nextDamageLogTime = Time.time + DamageLogInterval;
        string who = e.IsPlayer ? "P" : "E";
        Write("DMG " + who + " -" + e.Amount.ToString("F0"), pin: e.IsPlayer);
    }

    void OnEntityDead(EntityDeadEvent e)
    {
        if (e.IsPlayer)
        {
            Write("玩家死亡 lives=" + e.RemainingLives, pin: true);
            Write(e.RemainingLives > 0 ? "将复活" : "命尽回Start", pin: true);
            return;
        }

        Write("敌人死亡", pin: true);
    }

    void OnPlayerRespawned(PlayerRespawnedEvent e)
    {
        Write("玩家复活 " + Fv(e.Position) + " lives=" + e.RemainingLives, pin: true);
    }

    void OnEnemySpawned(EnemySpawnedEvent e)
    {
        Write("敌人生成", pin: true);
    }

    public void OnExitGame()
    {
        Write("退出导出...", pin: true);
        LogPoolSnapshot();
        if (logMemory)
            LogMemorySnapshot("退出", pin: true);

        string path = ExportLogFile();
        Write("已写入 " + path, pin: true);
        RefreshLogText();

        ComprehensiveTestSceneFlow.CleanupBeforeSceneChange();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void LogPoolSnapshot()
    {
        LogPoolStats(BulletPath, "Bullet");
        LogPoolStats(EnemyPath, "Enemy");

        Transform poolRuntime = GameObject.Find(PoolSceneRoots.RuntimeRootName)?.transform;
        if (poolRuntime == null)
        {
            Write("池根未创建", pin: true);
            return;
        }

        int activeBullets = 0;
        int activeEnemies = 0;
        for (int i = 0; i < poolRuntime.childCount; i++)
        {
            Transform child = poolRuntime.GetChild(i);
            if (child.name.StartsWith("Active_Bullets", StringComparison.Ordinal))
                activeBullets = CountActiveChildren(child);
            else if (child.name.StartsWith("Active_Enemies", StringComparison.Ordinal))
                activeEnemies = CountActiveChildren(child);
        }

        Write("池层级 ch=" + poolRuntime.childCount + " actB=" + activeBullets + " actE=" + activeEnemies, pin: true);
    }

    void LogPoolStats(string loadPath, string shortName)
    {
        if (!BundleResLoader.Instance.TryGetPool(loadPath, out PrefabPool pool))
        {
            Write("池未注册 " + shortName, pin: true);
            return;
        }

        Write("池 " + shortName + " b=" + pool.ActiveCount + " p=" + pool.InactiveCount, pin: true);
    }

    void CaptureMemoryBaseline()
    {
        baselineMonoUsed = Profiler.GetMonoUsedSizeLong();
        baselineTotalReserved = Profiler.GetTotalReservedMemoryLong();
        baselineGcManaged = GC.GetTotalMemory(false);
    }

    void LogMemorySnapshot(string label, bool pin = false)
    {
        long monoUsed = Profiler.GetMonoUsedSizeLong();
        long monoHeap = Profiler.GetMonoHeapSizeLong();
        long totalReserved = Profiler.GetTotalReservedMemoryLong();
        long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
        long gfxDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();
        long gcManaged = GC.GetTotalMemory(false);

        long monoDelta = monoUsed - baselineMonoUsed;
        long reservedDelta = totalReserved - baselineTotalReserved;
        long gcDelta = gcManaged - baselineGcManaged;

        Write(
            "MEM " + label +
            " m=" + Fb(monoUsed) + " d" + Fs(monoDelta) +
            " h=" + Fb(monoHeap) +
            " r=" + Fb(totalReserved) + " d" + Fs(reservedDelta) +
            " a=" + Fb(totalAllocated) +
            " g=" + Fb(gfxDriver) +
            " gc=" + Fb(gcManaged) + " d" + Fs(gcDelta),
            pin);

        CheckLeakTrend(monoUsed, totalReserved, gcManaged);
    }

    void CheckLeakTrend(long monoUsed, long totalReserved, long gcManaged)
    {
        monoUsedHistory.Add(monoUsed);
        if (monoUsedHistory.Count < LeakTrendSamples)
            return;

        long monoGrowth = monoUsed - monoUsedHistory[0];
        if (monoGrowth > LeakMonoGrowthThreshold)
            Write("MEM警告 mono近" + LeakTrendSamples + "次+" + Fb(monoGrowth), pin: true, isError: true);

        long reservedGrowth = totalReserved - baselineTotalReserved;
        long gcGrowth = gcManaged - baselineGcManaged;
        if (reservedGrowth > LeakMonoGrowthThreshold && monoGrowth > LeakMonoGrowthThreshold / 2)
            Write("MEM提示 res+" + Fb(reservedGrowth) + " gc+" + Fb(gcGrowth), pin: true);

        monoUsedHistory.RemoveAt(0);
    }

    string ExportLogFile()
    {
        var lines = new List<string>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
            lines.Add(entries[i].Line);

        return ComprehensiveTestLogExporter.ExportLog(lines, "PoolTest退出");
    }

    static int CountActiveChildren(Transform root)
    {
        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).gameObject.activeSelf)
                count++;
        }

        return count;
    }

    void Write(string message, bool pin = false, bool isError = false)
    {
        string line = "[" + Time.time.ToString("F1") + "s] " + message;

        if (logToConsole && (pin || isError))
        {
            if (isError)
                Debug.LogError(line);
            else
                Debug.Log(line);
        }

        entries.Add(new LogEntry { Line = line, Pin = pin });
        TrimEntries();
        RefreshLogText();
    }

    void TrimEntries()
    {
        int limit = maxStoredLines;
        while (entries.Count > limit)
        {
            int removeIndex = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].Pin)
                {
                    removeIndex = i;
                    break;
                }
            }

            if (removeIndex < 0)
                removeIndex = 0;

            entries.RemoveAt(removeIndex);
        }
    }

    void RefreshLogText()
    {
        if (logText == null)
            return;

        int start = Math.Max(0, entries.Count - maxUiLines);
        var sb = new StringBuilder();
        for (int i = start; i < entries.Count; i++)
            sb.AppendLine(entries[i].Line);

        logText.text = sb.ToString();
    }

    static string Fv(Vector3 v) => v.x.ToString("F0") + "," + v.y.ToString("F0") + "," + v.z.ToString("F0");

    static string Fb(long bytes)
    {
        if (bytes < 1024)
            return bytes + "B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024f).ToString("F0") + "K";
        return (bytes / (1024f * 1024f)).ToString("F1") + "M";
    }

    static string Fs(long bytes)
    {
        string sign = bytes >= 0 ? "+" : "-";
        long abs = Math.Abs(bytes);
        return sign + Fb(abs);
    }
}
