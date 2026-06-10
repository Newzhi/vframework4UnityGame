using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 并发引用计数测试：与 <see cref="Myloadtest"/> 同时 Load/Release 共享资源。
/// 禁止调用 UnloadAll；收尾仅 Release 本脚本持有的句柄并校验加载链路。
/// </summary>
public class MyLoadTest2 : MonoBehaviour
{
    const int CaseCount = 8;
    const string LogSource = "MyLoadTest2";

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
    Coroutine routine;
    int caseIndex;
    int currentCaseId;
    bool finishing;

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
            routine = StartCoroutine(RunCases());
    }

    IEnumerator RunCases()
    {
        logCollector?.BeginSession(LogSource);

        while (caseIndex < CaseCount)
        {
            currentCaseId = caseIndex;
            logCollector?.AppendLine("[" + LogSource + "] ---------- Case " + currentCaseId + " ----------");

            switch (currentCaseId)
            {
                case 0: CaseCatalogue(); break;
                case 1: CaseLoadPrefabConcurrent(); break;
                case 2: CaseReLoadPrefabRef(); break;
                case 3: CaseLoadSpriteVerify(); break;
                case 4: CaseCrossAtlas(); break;
                case 5: CaseCrossUI(); break;
                case 6: CaseReleaseAux(); break;
                case 7: CaseVerifyChainAndRelease(); break;
            }

            caseIndex++;
            float wait = currentCaseId == 0 ? 0f : intervalSeconds;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
        }

        logCollector?.NotifyRunnerComplete(LogSource);
        routine = null;
    }

    public void FinishTestAndSaveLog()
    {
        if (finishing)
            return;

        finishing = true;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        logCollector?.ForceEndSession();
        string path = logCollector?.LastSavedPath;
        Debug.Log("[MyLoadTest2] log saved: " + path);
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

    void CaseLoadPrefabConcurrent()
    {
        if (prefabRes == null)
            prefabRes = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");

        if (prefabRes == null)
        {
            LogFail("Load Prefab", "Model/Prefabs/tester");
            return;
        }

        Vector3 basePos = spawnRoot != null ? spawnRoot.position : Vector3.zero;
        Quaternion rot = spawnRoot != null ? spawnRoot.rotation : Quaternion.identity;
        float range = 1f;
        Vector3 randomOffset = new Vector3(
            Random.Range(-range, range),
            0f,
            Random.Range(-range, range)
        );

        instance = prefabRes.InstantiateAt(basePos + randomOffset + Vector3.forward * 2f, rot, null);
        if (instance == null)
        {
            LogFail("Load Prefab", "Instantiate failed");
            return;
        }

        LogOk("Load Prefab", "concurrent acquirer " + instance.name);
    }

    void CaseReLoadPrefabRef()
    {
        prefabResSecond = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");
        if (prefabResSecond == null)
        {
            LogFail("ReLoad Prefab", "second Load returned null");
            return;
        }

        if (prefabResSecond.GetAsset<GameObject>() == null)
        {
            LogFail("ReLoad Prefab", "GetAsset failed on cache hit");
            return;
        }

        LogOk("ReLoad Prefab", "cache hit + ref++ OK");
    }

    void CaseLoadSpriteVerify()
    {
        spriteRes = BundleResLoader.Instance.Load<Sprite>("Icon/3");
        if (spriteRes?.GetAsset<Sprite>() == null)
            LogFail("Load Sprite", "Icon/3");
        else
            LogOk("Load Sprite", "Icon/3 verified");
    }

    void CaseCrossAtlas()
    {
        atlasRes = BundleResLoader.Instance.Load<Sprite>("Atlas/Role/Hog_Attack_000");
        if (atlasRes?.GetAsset<Sprite>() == null)
            LogFail("Cross Atlas", "Atlas/Role/Hog_Attack_000");
        else
            LogOk("Cross Atlas", "atlas.bundle concurrent OK");
    }

    void CaseCrossUI()
    {
        uiRes = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot");
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

    void CaseVerifyChainAndRelease()
    {
        if (LogSource == LoadApiTestLogCollector.UnloadAllRunnerSource)
        {
            LogFail("UnloadAll Guard", "MyLoadTest2 must not call UnloadAll");
            return;
        }

        if (!BundleResLoader.Instance.EnsureReady())
        {
            LogFail("Verify Chain", "EnsureReady failed");
            return;
        }

        IAssetHandle probe = BundleResLoader.Instance.Load<Sprite>("Atlas/Role/Hog_Attack_000");
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

        LogOk("Verify Chain", "Load/Release OK; UnloadAll skipped (Myloadtest only)");
    }

    void LogOk(string api, string detail)
    {
        Debug.Log("[MyLoadTest2] OK | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError("[MyLoadTest2] FAIL | " + api + " | " + detail);
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
