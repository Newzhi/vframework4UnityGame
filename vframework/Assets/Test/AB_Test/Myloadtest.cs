using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AB 集成测试主脚本：Catalogue → Load → 跨包 → Release → 唯一 UnloadAll 收尾。
/// 与 <see cref="MyLoadTest2"/> 并发时，Case 8 会等待对方跑完再执行 UnloadAll。
/// </summary>
public class Myloadtest : MonoBehaviour
{
    const int CaseCount = 9;
    const string LogSource = "Myloadtest";
    const string PeerRunnerSource = "MyLoadTest2";

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
    Coroutine routine;
    int caseIndex;
    int currentCaseId;
    bool finishing;
    bool unloadAllDone;

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

            if (currentCaseId == 8)
            {
                yield return CaseUnloadAllAfterPeers();
            }
            else
            {
                switch (currentCaseId)
                {
                    case 0: CaseCatalogue(); break;
                    case 1: CaseLoadPrefab(); break;
                    case 2: CaseApplySprite(); break;
                    case 3: CaseReplaceMaterial(); break;
                    case 4: CaseCrossAtlas(); break;
                    case 5: CaseCrossUI(); break;
                    case 6: CaseReleaseAux(); break;
                    case 7: CaseDestroyPrefab(); break;
                }
            }

            caseIndex++;
            float wait = currentCaseId == 0 ? 0f : intervalSeconds;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
        }

        logCollector?.NotifyRunnerComplete(LogSource);
        routine = null;
    }

    public void LoadPrefabNow()
    {
        logCollector?.BeginSession(LogSource);
        currentCaseId = 1;
        caseIndex = 1;
        CaseLoadPrefab();
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
        Debug.Log("[Myloadtest] log saved: " + path);
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

    void CaseLoadPrefab()
    {
        if (instance != null)
        {
            LogOk("Load Prefab", "reuse " + instance.name);
            return;
        }

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

        instance = prefabRes.InstantiateAt(basePos + randomOffset, rot, null);
        if (instance == null)
        {
            LogFail("Load Prefab", "Instantiate failed");
            return;
        }

        if (spawnRoot != null)
            instance.transform.SetPositionAndRotation(spawnRoot.position, spawnRoot.rotation);

        LogOk("Load Prefab", instance.name);
    }

    void CaseApplySprite()
    {
        if (instance == null)
        {
            LogFail("Apply Sprite", "no instance");
            return;
        }

        spriteRes = BundleResLoader.Instance.Load<Sprite>("Icon/3");
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

    void CaseReplaceMaterial()
    {
        if (instance == null)
        {
            LogFail("Replace Material", "no instance");
            return;
        }

        materialRes = BundleResLoader.Instance.Load<Material>("Model/Materials/ReplaceMat");
        Material loadedMat = materialRes?.GetAsset<Material>();
        if (loadedMat == null)
        {
            LogFail("Replace Material", "Model/Materials/ReplaceMat");
            return;
        }

        instance.GetComponentInChildren<Renderer>().material = loadedMat;
        LogOk("Replace Material", "ReplaceMat");
    }

    void CaseCrossAtlas()
    {
        atlasRes = BundleResLoader.Instance.Load<Sprite>("Atlas/Role/Hog_Attack_000");
        if (atlasRes?.GetAsset<Sprite>() == null)
            LogFail("Cross Atlas", "Atlas/Role/Hog_Attack_000");
        else
            LogOk("Cross Atlas", "atlas.bundle + model.bundle");
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

    IEnumerator CaseUnloadAllAfterPeers()
    {
        if (logCollector != null
            && logCollector.expectedConcurrentRunners > 1
            && logCollector.IsRunnerRegistered(PeerRunnerSource))
        {
            float timeout = intervalSeconds * 12f;
            float elapsed = 0f;

            while (!logCollector.IsRunnerComplete(PeerRunnerSource) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!logCollector.IsRunnerComplete(PeerRunnerSource))
            {
                LogFail("UnloadAll", "timeout waiting for " + PeerRunnerSource);
                yield break;
            }

            LogOk("UnloadAll Wait", PeerRunnerSource + " finished");
        }

        if (logCollector != null && !logCollector.TryClaimUnloadAll(LogSource))
        {
            LogFail("UnloadAll", "only " + LoadApiTestLogCollector.UnloadAllRunnerSource + " may call UnloadAll");
            yield break;
        }

        if (!VerifyLoadChain())
        {
            LogFail("UnloadAll", "chain verification failed before UnloadAll");
            yield break;
        }

        BundleResLoader.Instance.UnloadAll();
        ClearLocalHandles();
        unloadAllDone = true;
        LogOk("UnloadAll", "exclusive cleanup done");
    }

    bool VerifyLoadChain()
    {
        if (!BundleResLoader.Instance.EnsureReady())
            return false;

        CatalogueReader catalogue = BundleResLoader.Instance.GetCatalogue();
        if (catalogue == null || !catalogue.IsLoaded)
            return false;

        IAssetHandle probe = BundleResLoader.Instance.Load<Sprite>("Icon/3");
        if (probe?.GetAsset<Sprite>() == null)
            return false;

        probe.Release();
        LogOk("Verify Chain", "Catalogue + Load/Release probe OK");
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
        Debug.Log("[Myloadtest] OK | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError("[Myloadtest] FAIL | " + api + " | " + detail);
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
