using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AB 简单集成测试：tester → Icon/3 换贴图 → ReplaceMat 换材质 → 释放 → 结束。
/// </summary>
public class Myloadtest : MonoBehaviour
{
    const int CaseCount = 7;

    public float intervalSeconds = 5f;
    public bool runOnStart = true;
    public LoadApiTestLogCollector logCollector;
    public Transform spawnRoot;
    public Button finishButton;
    public bool quitApplicationAfterSave = true;

    AbstractResource prefabRes;
    AbstractResource spriteRes;
    AbstractResource materialRes;
    GameObject instance;
    Coroutine routine;
    int caseIndex;
    int currentCaseId;
    bool finishing;

    void Awake()
    {
        if (logCollector == null)
            logCollector = GetComponent<LoadApiTestLogCollector>();
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
        logCollector?.BeginSession("Myloadtest");

        while (caseIndex < CaseCount)
        {
            currentCaseId = caseIndex;
            logCollector?.AppendLine("---------- Case " + currentCaseId + " ----------");

            switch (currentCaseId)
            {
                case 0: CaseCatalogue(); break;
                case 1: CaseLoadPrefab(); break;
                case 2: CaseApplySprite(); break;
                case 3: CaseReplaceMaterial(); break;
                case 4: CaseReleaseAux(); break;
                case 5: CaseDestroyPrefab(); break;
                case 6: CaseUnloadAll(); break;
            }

            caseIndex++;
            float wait = currentCaseId == 0 ? 0f : intervalSeconds;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
        }

        logCollector?.SaveSessionAndGetPath();
        routine = null;
    }

    public void LoadPrefabNow()
    {
        logCollector?.BeginSession("Myloadtest");
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

        string path = logCollector?.SaveSessionAndGetPath();
        Debug.Log("[Myloadtest] log saved: " + path);
        logCollector?.AppendLine("Finish: " + path);

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

        instance = prefabRes.Instantiate();
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

    void CaseReleaseAux()
    {
        spriteRes?.Release();
        spriteRes = null;
        materialRes?.Release();
        materialRes = null;
        LogOk("Release", "sprite + material released");
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

    void CaseUnloadAll()
    {
        BundleResLoader.Instance.UnloadAll();
        LogOk("Finish", "UnloadAll done");
    }

    void LogOk(string api, string detail)
    {
        Debug.Log("[Myloadtest] OK | " + api + " | " + detail);
        logCollector?.Record(currentCaseId, caseIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError("[Myloadtest] FAIL | " + api + " | " + detail);
        logCollector?.Record(currentCaseId, caseIndex, api, false, detail);
    }

    void OnDestroy()
    {
        if (instance != null)
            Destroy(instance);

        spriteRes?.Release();
        materialRes?.Release();
        prefabRes?.Release();
        logCollector?.EndSession();
    }
}
