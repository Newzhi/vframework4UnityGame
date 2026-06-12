using System.Collections;
using UnityEngine;

/// <summary>
/// Load / Release 压力测试（单 Runner，默认独立套系）。
/// 后续可与其他 Runner 同挂做三端并发：增大 Collector.expectedConcurrentRunners，
/// 并在 Myloadtest.CaseUnloadAllAfterPeers 中等待本 Runner 完成。
/// </summary>
public class MyLoadStressTest : AbLoadTestRunnerBase
{
    const int TotalCases = 4;

    [Header("Stress")]
    [Tooltip("Case 1/2 循环次数")]
    public int stressCycles = 20;

    [Tooltip("压力测试使用的 Catalogue 简路径")]
    public string[] stressLoadPaths = { "Icon/3", "Atlas/Role/Hog_Attack_000" };

    protected override string LogSource => "MyLoadStressTest";
    protected override int CaseCount => TotalCases;

    protected override IEnumerator RunCase(int caseId)
    {
        switch (caseId)
        {
            case 0:
                CaseWarmup();
                yield break;
            case 1:
                yield return CaseRapidLoadRelease();
                yield break;
            case 2:
                yield return CaseMultiHandleBurst();
                yield break;
            case 3:
                CaseStressUnloadAll();
                yield break;
        }
    }

    void CaseWarmup()
    {
        if (BundleResLoader.Instance.EnsureReady())
            LogOk("Warmup", "EnsureReady OK cycles=" + stressCycles);
        else
            LogFail("Warmup", "EnsureReady failed");
    }

    IEnumerator CaseRapidLoadRelease()
    {
        int cycles = Mathf.Max(1, stressCycles);
        int failCount = 0;

        for (int i = 0; i < cycles; i++)
        {
            string path = stressLoadPaths[i % stressLoadPaths.Length];
            IAssetHandle handle = BundleResLoader.Instance.Load<Sprite>(path);
            if (handle == null)
            {
                failCount++;
                continue;
            }

            if (handle.GetAsset<Sprite>() == null)
                failCount++;

            handle.Release();
            yield return null;
        }

        if (failCount == 0)
            LogOk("Stress Rapid LoadRelease", "cycles=" + cycles + " paths=" + stressLoadPaths.Length);
        else
            LogFail("Stress Rapid LoadRelease", "failCount=" + failCount + "/" + cycles);
    }

    IEnumerator CaseMultiHandleBurst()
    {
        int burst = Mathf.Min(5, stressLoadPaths.Length);
        IAssetHandle[] handles = new IAssetHandle[burst];

        for (int i = 0; i < burst; i++)
        {
            handles[i] = BundleResLoader.Instance.Load<Sprite>(stressLoadPaths[i]);
            if (handles[i] == null)
            {
                LogFail("Stress MultiHandle", "load failed " + stressLoadPaths[i]);
                yield break;
            }

            yield return null;
        }

        for (int i = 0; i < burst; i++)
        {
            handles[i]?.Release();
            handles[i] = null;
        }

        LogOk("Stress MultiHandle", "held=" + burst + " then released");
    }

    void CaseStressUnloadAll()
    {
        if (logCollector != null && !logCollector.TryClaimUnloadAll(LogSource))
        {
            LogFail("Stress UnloadAll", "claim failed; set unloadAllRunnerSource=" + LogSource + " for standalone run");
            return;
        }

        BundleResLoader.Instance.UnloadAll();
        LogOk("Stress UnloadAll", "cleanup done");
    }
}
