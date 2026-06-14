using System;
using System.Collections.Generic;
using System.Text;
using BaseFramework.BaseEventSys;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 综合测试专用日志：事件总线、对象池快照（b/p/ref/max + Hierarchy 交叉校验）、内存趋势。
/// 重要事件（死亡/复活/系统/池快照）优先保留，支持长时间对局导出。
/// </summary>
public class ComprehensiveTestLogger : MonoBehaviour
{
    const string BulletPath = "Model/Prefabs/Bullet";
    const string EnemyPath = "Model/Prefabs/enemy";
    const int EnemyExpectedRefCount = 1;
    const int BulletBaseMaxInactive = 48;
    const int EnemyBaseMaxInactive = 12;
    const float DamageLogInterval = 1f;
    const float SnapshotInterval = 5f;
    const int LeakTrendSamples = 6;
    const long LeakMonoGrowthThreshold = 5 * 1024 * 1024;
    const int DefaultMaxStoredLines = 12000;
    const string LoggerSchemaVersion = "v2-ref-holders-max";

    public static ComprehensiveTestLogger Instance { get; private set; }

    /// <summary>子弹池 ref 变化时由 gameplay 或事件回调打点（Pin，不被截断）。</summary>
    public static void LogBulletPoolRef(string reason)
    {
        if (Instance != null)
            Instance.WriteBulletPoolRef(reason);
    }

    public static void LogEnemySpawnMode(ComprehensiveTestDebugConfig.EnemySpawnMode mode)
    {
        if (Instance != null)
            Instance.Write("敌人生成模式=" + mode, pin: true);
    }

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
        Instance = this;

        if (logText == null)
            logText = GameObject.Find("Log")?.GetComponent<Text>();

        if (exitButton == null)
            exitButton = GameObject.Find("ExitGame")?.GetComponent<Button>();

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitGame);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

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

        Write("Logger启动 schema=" + LoggerSchemaVersion, pin: true);
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
        if (ComprehensiveTestDebugConfig.ResolveEnemySpawnMode()
            == ComprehensiveTestDebugConfig.EnemySpawnMode.DirectInstantiate)
            WriteBulletPoolRef("敌人死亡将Destroy卸子弹份额");
        else
            WriteBulletPoolRef("敌人死亡(池化回敌人池,子弹ref不降)");
    }

    void OnPlayerRespawned(PlayerRespawnedEvent e)
    {
        Write("玩家复活 " + Fv(e.Position) + " lives=" + e.RemainingLives, pin: true);
    }

    void OnEnemySpawned(EnemySpawnedEvent e)
    {
        Write("敌人生成", pin: true);
        WriteBulletPoolRef("敌人生成后");
    }

    void WriteBulletPoolRef(string reason)
    {
        int holders = PlayerTest.BulletPoolShareCount + enemyTest.BulletPoolShareCount;
        if (!PrefabPoolManager.Instance.TryGetPool(BulletPath, out PrefabPool pool))
        {
            Write("池ref[" + reason + "] 未注册 holders=" + holders, pin: true);
            return;
        }

        Write(
            "池ref[" + reason + "] ref=" + pool.RefCount + " max=" + pool.MaxInactiveCapacity +
            " holders=" + holders + " b=" + pool.ActiveCount + " p=" + pool.InactiveCount,
            pin: true);

        if (pool.RefCount != holders)
            Write(
                "池ref[" + reason + "] 持有者不符 holders=" + holders + " ref=" + pool.RefCount,
                pin: true,
                isError: true);
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
        Scene active = SceneManager.GetActiveScene();
        int expectedBulletRef = PlayerTest.BulletPoolShareCount + enemyTest.BulletPoolShareCount;
        LogPoolStats(BulletPath, "Bullet", expectedBulletRef, BulletBaseMaxInactive);

        if (ComprehensiveTestDebugConfig.ResolveEnemySpawnMode()
            == ComprehensiveTestDebugConfig.EnemySpawnMode.Pooled)
            LogPoolStats(EnemyPath, "Enemy", EnemyExpectedRefCount, EnemyBaseMaxInactive);

        if (!PoolSceneRootsUtil.TryGetRuntimeRoot(active, out Transform poolRuntime))
        {
            Write("池根未创建 scene=" + active.name, pin: true);
            return;
        }

        bool sameScene = poolRuntime.gameObject.scene == active;
        Write("池 scene=" + active.name + " ch=" + poolRuntime.childCount + " rootInActive=" + sameScene, pin: true);
        LogHierarchyCrossCheck(active, BulletPath, "Bullet");
        LogHierarchyCrossCheck(active, EnemyPath, "Enemy");
    }

    /// <summary>
    /// 对比 PrefabPool 计数与 Hierarchy 下 Pool_* 子节点 activeSelf 数量（单父节点方案）。
    /// </summary>
    void LogHierarchyCrossCheck(Scene scene, string loadPath, string shortName)
    {
        if (!PrefabPoolManager.Instance.TryGetPool(loadPath, out PrefabPool pool))
            return;

        if (!PoolSceneRootsUtil.TryGetPoolRoot(loadPath, scene, out Transform poolRoot))
            return;

        int activeInHierarchy = CountActiveChildren(poolRoot);
        int totalInHierarchy = poolRoot.childCount;
        int pooledInHierarchy = totalInHierarchy - activeInHierarchy;

        if (activeInHierarchy != pool.ActiveCount || pooledInHierarchy != pool.InactiveCount)
            Write(
                "池校验 " + shortName + " hierA=" + activeInHierarchy + " hierP=" + pooledInHierarchy +
                " poolB=" + pool.ActiveCount + " poolP=" + pool.InactiveCount,
                pin: true,
                isError: true);
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

    /// <summary>
    /// 读取 PrefabPool.ActiveCount / InactiveCount / RefCount / MaxInactiveCapacity；校验本场景预期 ref 与闲置上限。
    /// </summary>
    void LogPoolStats(string loadPath, string shortName, int expectedRefCount, int baseMaxInactive)
    {
        if (!PrefabPoolManager.Instance.TryGetPool(loadPath, out PrefabPool pool))
        {
            Write("池未注册 " + shortName, pin: true);
            return;
        }

        // ActiveCount = 已 GetObj 未 RecycleObj；InactiveCount = 闲置队列深度；RefCount = GetOrCreatPool 次数
        Write(
            "池 " + shortName + " b=" + pool.ActiveCount + " p=" + pool.InactiveCount +
            " ref=" + pool.RefCount + " max=" + pool.MaxInactiveCapacity +
            (shortName == "Bullet"
                ? " holders=" + (PlayerTest.BulletPoolShareCount + enemyTest.BulletPoolShareCount)
                : ""),
            pin: true);

        if (pool.RefCount != expectedRefCount)
            Write(
                "池 " + shortName + " ref异常 exp=" + expectedRefCount + " act=" + pool.RefCount,
                pin: true,
                isError: true);

        if (shortName == "Bullet")
        {
            int holderCount = PlayerTest.BulletPoolShareCount + enemyTest.BulletPoolShareCount;
            if (pool.RefCount != holderCount)
                Write(
                    "池 Bullet 持有者不符 holders=" + holderCount + " ref=" + pool.RefCount,
                    pin: true,
                    isError: true);
        }

        int expectedMaxForRef = baseMaxInactive > 0 && pool.RefCount > 0
            ? baseMaxInactive * pool.RefCount
            : 0;
        if (expectedMaxForRef > 0 && pool.MaxInactiveCapacity != expectedMaxForRef)
            Write(
                "池 " + shortName + " max异常 exp=" + expectedMaxForRef + " act=" + pool.MaxInactiveCapacity,
                pin: true,
                isError: true);

        if (pool.MaxInactiveCapacity > 0 && pool.InactiveCount > pool.MaxInactiveCapacity)
            Write("池 " + shortName + " 闲置超max", pin: true, isError: true);
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
