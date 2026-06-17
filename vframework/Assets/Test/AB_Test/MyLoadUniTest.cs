using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// LoadUniTaskAsync 集成测试主脚本：与 <see cref="MyLoadUniTest2"/> 并发跑同一套 Case。
/// 默认在 TestABScene 上禁用；启用前请关闭 Myloadtest / MyLoadTest2，并改 Collector 为异步套系配置。
/// </summary>
public class MyLoadUniTest : MonoBehaviour
{
    const int CaseCount = 10;
    const string LogSource = "MyLoadUniTest";
    const string PeerRunnerSource = "MyLoadUniTest2";

    public float intervalSeconds = 5f;
    public bool runOnStart = true;
    public LoadApiTestLogCollector logCollector;
    public Transform spawnRoot;
    public Button finishButton;
    public bool quitApplicationAfterSave = true;

    IAssetHandle prefabRes;
    IAssetHandle spriteRes;
    IAssetHandle materialRes;
    IAssetHandle atlasRes;
    IAssetHandle uiRes;
    GameObject instance;
    GameObject uiInstance;
    int caseIndex;
    int currentCaseId;
    bool finishing;
    bool unloadAllDone;
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

            if (currentCaseId == 9)
                await CaseUnloadAllAfterPeersAsync();
            else
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
            case 1: await CaseLoadPrefabAsync(); break;
            case 2: await CaseApplySpriteAsync(); break;
            case 3: await CaseReplaceMaterialAsync(); break;
            case 4: await CaseCrossAtlasAsync(); break;
            case 5: await CaseCrossUIAsync(); break;
            case 6: CaseReleaseAux(); break;
            case 7: CaseDestroyPrefab(); break;
            case 8: await CaseInflightAbandonAsync(); break;
        }
    }

    public void FinishTestAndSaveLog()
    {
        if (finishing)
            return;

        finishing = true;
        logCollector?.ForceEndSession();
        string path = logCollector?.LastSavedPath;
        Debug.Log("[MyLoadUniTest] log saved: " + path);
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

    async UniTask CaseLoadPrefabAsync()
    {
        if (instance != null)
        {
            LogOk("LoadUni Prefab", "reuse " + instance.name);
            return;
        }

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

        instance = prefabRes.InstantiateAt(basePos + randomOffset, rot, null);
        if (instance == null)
        {
            LogFail("LoadUni Prefab", "Instantiate failed");
            return;
        }

        if (spawnRoot != null)
            instance.transform.SetPositionAndRotation(spawnRoot.position, spawnRoot.rotation);

        LogOk("LoadUni Prefab", instance.name);
    }

    async UniTask CaseApplySpriteAsync()
    {
        if (instance == null)
        {
            LogFail("Apply Sprite", "no instance");
            return;
        }

        spriteRes = await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>("Icon/3");
        Texture tex = spriteRes?.GetAsset<Sprite>()?.texture;
        if (tex == null)
        {
            LogFail("Apply Sprite", "Icon/3");
            return;
        }

        Material mat = instance.GetComponentInChildren<Renderer>().material;
        mat.SetTexture("_BaseMap", tex);
        mat.mainTexture = tex;
        LogOk("Apply Sprite", "Icon/3");
    }

    async UniTask CaseReplaceMaterialAsync()
    {
        if (instance == null)
        {
            LogFail("Replace Material", "no instance");
            return;
        }

        materialRes = await BundleResLoader.Instance.LoadUniTaskAsync<Material>("Model/Materials/ReplaceMat");
        Material loadedMat = materialRes?.GetAsset<Material>();
        if (loadedMat == null)
        {
            LogFail("Replace Material", "Model/Materials/ReplaceMat");
            return;
        }

        instance.GetComponentInChildren<Renderer>().material = loadedMat;
        LogOk("Replace Material", "ReplaceMat");
    }

    async UniTask CaseCrossAtlasAsync()
    {
        atlasRes = await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>("Atlas/Role/Hog_Attack_000");
        if (atlasRes?.GetAsset<Sprite>() == null)
            LogFail("Cross Atlas", "Atlas/Role/Hog_Attack_000");
        else
            LogOk("Cross Atlas", "atlas.bundle + model.bundle");
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
            uiInstance.transform.position = spawnRoot.position + Vector3.right * 3f;

        LogOk("Cross UI", "ui.bundle + model.bundle");
    }

    void CaseReleaseAux()
    {
        spriteRes?.Release();
        spriteRes = null;
        materialRes?.Release();
        materialRes = null;
        atlasRes?.Release();
        atlasRes = null;

        if (uiInstance != null)
        {
            Destroy(uiInstance);
            uiInstance = null;
        }

        uiRes?.Release();
        uiRes = null;
        LogOk("Release", "sprite/material/atlas/ui released");
    }

    void CaseDestroyPrefab()
    {
        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }

        prefabRes?.Release();
        prefabRes = null;
        LogOk("Destroy Prefab", "instance destroyed");
    }

    async UniTask CaseInflightAbandonAsync()
    {
        // 与 MyLoadUniTest2 Case 8（InflightParallel 用 tester）错开路径，避免双 Runner 同 Case 抢同 key
        const string path = "Icon/3";
        spriteRes?.Release();
        spriteRes = null;
        await UniTask.Yield(PlayerLoopTiming.Update);

        bool ok = await BundleResLoader.Instance.VerifyInflightAbandonAsync<Sprite>(path);
        if (ok)
            LogOk("InflightAbandon", "ref==0 discard; cache empty for " + path);
        else
            LogFail("InflightAbandon", "abandon verification failed for " + path);
    }

    async UniTask CaseUnloadAllAfterPeersAsync()
    {
        if (logCollector != null
            && logCollector.expectedConcurrentRunners > 1
            && logCollector.IsRunnerRegistered(PeerRunnerSource))
        {
            float timeout = intervalSeconds * 12f;
            float elapsed = 0f;

            while (!logCollector.IsRunnerComplete(PeerRunnerSource) && elapsed < timeout)
            {
                await UniTask.Yield();
                elapsed += Time.deltaTime;
            }

            if (!logCollector.IsRunnerComplete(PeerRunnerSource))
            {
                LogFail("UnloadAll", "timeout waiting for " + PeerRunnerSource);
                return;
            }

            LogOk("UnloadAll Wait", PeerRunnerSource + " finished");
        }

        if (logCollector != null && !logCollector.TryClaimUnloadAll(LogSource))
        {
            LogFail("UnloadAll", "only " + logCollector.AllowedUnloadAllRunner + " may call UnloadAll");
            return;
        }

        if (!await VerifyLoadChainAsync())
        {
            LogFail("UnloadAll", "chain verification failed before UnloadAll");
            return;
        }

        BundleResLoader.Instance.UnloadAll();
        ClearLocalHandles();
        unloadAllDone = true;
        LogOk("UnloadAll", "exclusive cleanup done");
    }

    async UniTask<bool> VerifyLoadChainAsync()
    {
        if (!BundleResLoader.Instance.EnsureReady())
            return false;

        CatalogueReader catalogue = BundleResLoader.Instance.GetCatalogue();
        if (catalogue == null || !catalogue.IsLoaded)
            return false;

        IAssetHandle probe = await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>("Icon/3");
        if (probe?.GetAsset<Sprite>() == null)
            return false;

        probe.Release();
        LogOk("Verify Chain", "Catalogue + LoadUniTaskAsync probe OK");
        return true;
    }

    void ClearLocalHandles()
    {
        instance = null;
        uiInstance = null;
        prefabRes = null;
        spriteRes = null;
        materialRes = null;
        atlasRes = null;
        uiRes = null;
    }

    void LogOk(string api, string detail)
    {
        Debug.Log("[MyLoadUniTest] OK | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError("[MyLoadUniTest] FAIL | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, false, detail);
    }

    void OnDestroy()
    {
        if (instance != null)
            Destroy(instance);

        if (unloadAllDone)
            return;

        spriteRes?.Release();
        materialRes?.Release();
        atlasRes?.Release();
        uiRes?.Release();
        prefabRes?.Release();
        logCollector?.NotifyRunnerComplete(LogSource);
    }
}
