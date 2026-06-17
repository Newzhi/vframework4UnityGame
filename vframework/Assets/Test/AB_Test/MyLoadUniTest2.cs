using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// LoadUniTaskAsync 并发引用计数测试：与 <see cref="MyLoadUniTest"/> 同时 await Load/Release。
/// 禁止调用 UnloadAll。默认在 TestABScene 上禁用。
/// </summary>
public class MyLoadUniTest2 : MonoBehaviour
{
    const int CaseCount = 9;
    const string LogSource = "MyLoadUniTest2";

    public float intervalSeconds = 5f;
    public bool runOnStart = true;
    public LoadApiTestLogCollector logCollector;
    public Transform spawnRoot;
    public Button finishButton;
    public bool quitApplicationAfterSave = true;

    IAssetHandle prefabRes;
    IAssetHandle prefabResSecond;
    IAssetHandle spriteRes;
    IAssetHandle atlasRes;
    IAssetHandle uiRes;
    GameObject instance;
    GameObject uiInstance;
    int caseIndex;
    int currentCaseId;
    bool finishing;
    bool running;

    void Awake()
    {
        if (logCollector == null)
            logCollector = GetComponent<LoadApiTestLogCollector>();
        logCollector = LoadApiTestLogCollector.EnsureShared(logCollector);
    }

    void Start()
    {
        if (finishButton == null)
        {
            GameObject exitGo = GameObject.Find("ExitBtn");
            if (exitGo != null)
                finishButton = exitGo.GetComponent<Button>();
        }

        if (finishButton != null)
        {
            finishButton.onClick.RemoveListener(FinishTestAndSaveLog);
            finishButton.onClick.AddListener(FinishTestAndSaveLog);
        }

        if (runOnStart)
            RunCasesAsync().Forget();
    }

    async UniTaskVoid RunCasesAsync()
    {
        if (running)
            return;

        running = true;
        logCollector?.BeginSession(LogSource);

        while (caseIndex < CaseCount)
        {
            currentCaseId = caseIndex;
            logCollector?.AppendLine("[" + LogSource + "] ---------- Case " + currentCaseId + " ----------");

            await RunCaseAsync(currentCaseId);

            caseIndex++;
            float wait = currentCaseId == 0 ? 0f : intervalSeconds;
            if (wait > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(wait));
        }

        logCollector?.NotifyRunnerComplete(LogSource);
        running = false;
    }

    async UniTask RunCaseAsync(int caseId)
    {
        switch (caseId)
        {
            case 0: CaseCatalogue(); break;
            case 1: await CaseLoadPrefabConcurrentAsync(); break;
            case 2: await CaseReLoadPrefabRefAsync(); break;
            case 3: await CaseLoadSpriteVerifyAsync(); break;
            case 4: await CaseCrossAtlasAsync(); break;
            case 5: await CaseCrossUIAsync(); break;
            case 6: CaseReleaseAux(); break;
            case 7: await CaseVerifyChainAndReleaseAsync(); break;
            case 8: await CaseInflightParallelAsync(); break;
        }
    }

    public void FinishTestAndSaveLog()
    {
        if (finishing)
            return;

        finishing = true;
        logCollector?.ForceEndSession();
        string path = logCollector?.LastSavedPath;
        Debug.Log("[MyLoadUniTest2] log saved: " + path);
        logCollector?.AppendLine("[" + LogSource + "] Finish: " + path);

        if (!quitApplicationAfterSave)
            return;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CaseCatalogue()
    {
        if (BundleResLoader.Instance.EnsureReady())
            LogOk("Catalogue", BundleResLoader.GetDefaultRuntimeBundleRoot());
        else
            LogFail("Catalogue", "EnsureReady failed");
    }

    async UniTask CaseLoadPrefabConcurrentAsync()
    {
        if (prefabRes == null)
            prefabRes = await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>("Model/Prefabs/tester");

        if (prefabRes == null)
        {
            LogFail("LoadUni Prefab", "Model/Prefabs/tester");
            return;
        }

        Vector3 basePos = spawnRoot != null ? spawnRoot.position : Vector3.zero;
        Quaternion rot = spawnRoot != null ? spawnRoot.rotation : Quaternion.identity;
        float range = 1f;
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-range, range),
            0f,
            UnityEngine.Random.Range(-range, range));

        instance = prefabRes.InstantiateAt(basePos + randomOffset + Vector3.forward * 2f, rot, null);
        if (instance == null)
        {
            LogFail("LoadUni Prefab", "Instantiate failed");
            return;
        }

        LogOk("LoadUni Prefab", "concurrent acquirer " + instance.name);
    }

    async UniTask CaseReLoadPrefabRefAsync()
    {
        prefabResSecond = await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>("Model/Prefabs/tester");
        if (prefabResSecond == null)
        {
            LogFail("ReLoadUni Prefab", "second LoadUniTaskAsync returned null");
            return;
        }

        if (prefabResSecond.GetAsset<GameObject>() == null)
        {
            LogFail("ReLoadUni Prefab", "GetAsset failed on cache hit");
            return;
        }

        LogOk("ReLoadUni Prefab", "cache hit + ref++ OK");
    }

    async UniTask CaseLoadSpriteVerifyAsync()
    {
        spriteRes = await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>("Icon/3");
        if (spriteRes?.GetAsset<Sprite>() == null)
            LogFail("LoadUni Sprite", "Icon/3");
        else
            LogOk("LoadUni Sprite", "Icon/3 verified");
    }

    async UniTask CaseCrossAtlasAsync()
    {
        atlasRes = await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>("Atlas/Role/Hog_Attack_000");
        if (atlasRes?.GetAsset<Sprite>() == null)
            LogFail("Cross Atlas", "Atlas/Role/Hog_Attack_000");
        else
            LogOk("Cross Atlas", "atlas.bundle concurrent OK");
    }

    async UniTask CaseCrossUIAsync()
    {
        uiRes = await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>("UI/UIRoot");
        if (uiRes == null)
        {
            LogFail("Cross UI", "UI/UIRoot load");
            return;
        }

        uiInstance = uiRes.Instantiate();
        if (uiInstance == null)
        {
            LogFail("Cross UI", "UI/UIRoot instantiate");
            return;
        }

        if (spawnRoot != null)
            uiInstance.transform.position = spawnRoot.position + Vector3.left * 3f;

        LogOk("Cross UI", "ui.bundle concurrent OK");
    }

    void CaseReleaseAux()
    {
        spriteRes?.Release();
        spriteRes = null;
        atlasRes?.Release();
        atlasRes = null;

        if (uiInstance != null)
        {
            Destroy(uiInstance);
            uiInstance = null;
        }

        uiRes?.Release();
        uiRes = null;
        LogOk("Release", "sprite/atlas/ui released (prefab kept)");
    }

    async UniTask CaseVerifyChainAndReleaseAsync()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            LogFail("Verify Chain", "EnsureReady failed");
            return;
        }

        IAssetHandle probe = await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>("Atlas/Role/Hog_Attack_000");
        if (probe?.GetAsset<Sprite>() == null)
        {
            LogFail("Verify Chain", "Atlas probe failed");
            return;
        }

        probe.Release();

        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }

        prefabResSecond?.Release();
        prefabResSecond = null;
        prefabRes?.Release();
        prefabRes = null;

        LogOk("Verify Chain", "LoadUniTaskAsync OK; UnloadAll skipped (MyLoadUniTest only)");
    }

    async UniTask CaseInflightParallelAsync()
    {
        const string path = "Model/Prefabs/tester";
        prefabRes?.Release();
        prefabRes = null;
        prefabResSecond?.Release();
        prefabResSecond = null;
        await UniTask.Yield(PlayerLoopTiming.Update);

        (IAssetHandle first, IAssetHandle second) = await UniTask.WhenAll(
            BundleResLoader.Instance.LoadUniTaskAsync<GameObject>(path),
            BundleResLoader.Instance.LoadUniTaskAsync<GameObject>(path));

        if (first == null || second == null)
        {
            LogFail("InflightParallel", "parallel LoadUniTaskAsync returned null");
            return;
        }

        GameObject assetA = first.GetAsset<GameObject>();
        GameObject assetB = second.GetAsset<GameObject>();
        if (assetA == null || assetB == null)
        {
            LogFail("InflightParallel", "GetAsset failed");
            first.Release();
            second.Release();
            return;
        }

        if (!ReferenceEquals(assetA, assetB))
        {
            LogFail("InflightParallel", "merged load did not share same asset instance");
            first.Release();
            second.Release();
            return;
        }

        prefabRes = first;
        second.Release();
        LogOk("InflightParallel", "same-path inFlight merge OK; ref shared asset");
    }

    void LogOk(string api, string detail)
    {
        Debug.Log("[MyLoadUniTest2] OK | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError("[MyLoadUniTest2] FAIL | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, false, detail);
    }

    void OnDestroy()
    {
        if (instance != null)
            Destroy(instance);

        spriteRes?.Release();
        atlasRes?.Release();
        uiRes?.Release();
        prefabResSecond?.Release();
        prefabRes?.Release();
        logCollector?.NotifyRunnerComplete(LogSource);
    }
}
