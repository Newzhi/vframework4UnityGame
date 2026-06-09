using System.Collections;
using UnityEngine;

/// <summary>
/// AB 加载集成测试：Prefab 实例化 → 贴图换材质 → 材质替换 → 释放资源 → 结束。
/// 需 DeviceDebug 打包后 Play；日志见 LoadApiTestLogCollector。
/// </summary>
public class LoadApiTester : MonoBehaviour
{
    const string LogPrefix = "[LoadApiTester]";
    const int ScenarioCaseCount = 10;

    [Header("循环")]
    [Tooltip("每条用例之间的间隔（秒）")]
    public float intervalSeconds = 5f;

    public bool runOnStart = true;

    [Tooltip("集成场景跑完 10 步后是否从头再来")]
    public bool loop = false;

    [Header("日志")]
    public LoadApiTestLogCollector logCollector;

    [Header("场景资源路径")]
    public string prefabLoadPath = "Model/Prefabs/Ji";
    public string spriteLoadPath = "Atlas/Role/Hog_Attack_000";
    public string materialLoadPath = "Model/ji/cai/lambert2";

    [Header("API 辅助用例")]
    public string spriteAssetPath = "Assets/AssetBundle/Atlas/Role/Hog_Attack_001.png";
    public string spriteBundleName = "atlas.bundle";
    public string spriteAssetName = "Hog_Attack_002";
    public string invalidLoadPath = "Not/Exist/Resource";

    [Header("Prefab 实例化")]
    public Transform spawnRoot;
    public float spawnOffsetX = 2f;

    [Header("结束运行")]
    [Tooltip("10 步跑完后写 JSON 并退出（Editor 下停止 Play）")]
    public bool quitAfterScenarioComplete = true;

    [Tooltip("按 Esc 提前结束：写 JSON，清理资源")]
    public bool enableEscToStop = true;

    [Tooltip("Esc 结束时可否退出应用（真机包）")]
    public bool quitOnEscStop;

    [Header("材质贴图")]
    [Tooltip("留空则用 Material.mainTexture")]
    public string textureShaderProperty = "_MainTex";

    int caseIndex;
    int currentCaseId;
    AbstractResource prefabRes;
    AbstractResource spriteRes;
    AbstractResource materialRes;
    GameObject prefabInstance;
    Renderer targetRenderer;
    Coroutine testCoroutine;

    void Awake()
    {
        if (logCollector == null)
            logCollector = GetComponent<LoadApiTestLogCollector>();
    }

    void Start()
    {
        if (runOnStart)
            StartTestsInternal();
    }

    void Update()
    {
        if (!enableEscToStop || testCoroutine == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            StopTestsAndFinish(quitOnEscStop);
    }

    void OnDestroy()
    {
        CleanupScenario();
        logCollector?.EndSession();
    }

    [ContextMenu("Start Tests")]
    public void StartTests()
    {
        StartTestsInternal();
    }

    void StartTestsInternal()
    {
        if (testCoroutine != null)
            StopCoroutine(testCoroutine);

        CleanupScenario();
        caseIndex = 0;
        logCollector?.BeginSession();
        testCoroutine = StartCoroutine(RunTestsLoop());
    }

    [ContextMenu("Stop Tests")]
    public void StopTests()
    {
        StopTestsAndFinish(false);
    }

    void StopTestsAndFinish(bool quitApp)
    {
        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
            testCoroutine = null;
        }

        CleanupScenario();
        logCollector?.EndSession();

        if (quitApp)
            QuitApplication();
    }

    static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator RunTestsLoop()
    {
        do
        {
            RunNextCase();
            yield return new WaitForSeconds(intervalSeconds);
        } while (loop);

        testCoroutine = null;
        logCollector?.EndSession();

        if (quitAfterScenarioComplete && !loop)
        {
            Debug.Log(LogPrefix + " Scenario complete, quitting...");
            QuitApplication();
        }
    }

    void RunNextCase()
    {
        currentCaseId = caseIndex % ScenarioCaseCount;

        Debug.Log(LogPrefix + " ---------- Case " + currentCaseId + " (round " + caseIndex + ") ----------");

        switch (currentCaseId)
        {
            case 0:
                TestCatalogueLoaded();
                break;
            case 1:
                TestLoadPrefabAndInstantiate();
                break;
            case 2:
                TestApplySpriteToMaterial();
                break;
            case 3:
                TestReplaceMaterial();
                break;
            case 4:
                TestLoadCacheHit();
                break;
            case 5:
                TestLoadByAssetPathAndBundle();
                break;
            case 6:
                TestLoadInvalidPath();
                break;
            case 7:
                TestReleaseReplacedResources();
                break;
            case 8:
                TestDestroyPrefabInstance();
                break;
            case 9:
                TestFinishAndUnloadAll();
                break;
        }

        caseIndex++;
    }

    void TestCatalogueLoaded()
    {
        BundleResLoader loader = BundleResLoader.Instance;
        if (!loader.EnsureReady())
        {
            LogFail("Catalogue", "EnsureReady failed");
            return;
        }

        if (!loader.IsCatalogueLoaded)
        {
            LogFail("Catalogue", "not loaded after EnsureReady");
            return;
        }

        int count = loader.GetCatalogue().Catalog.entries?.Length ?? 0;
        LogOk("Catalogue", "entries=" + count + ", root=" + BundleResLoader.GetDefaultRuntimeBundleRoot());
    }

    void TestLoadPrefabAndInstantiate()
    {
        CleanupPrefabInstance();
        prefabRes?.Release();
        prefabRes = null;
        targetRenderer = null;

        prefabRes = BundleResLoader.Instance.Load<GameObject>(prefabLoadPath);
        if (prefabRes == null)
        {
            LogFail("Load Prefab", prefabLoadPath);
            return;
        }

        GameObject prefab = prefabRes.GetAsset<GameObject>();
        if (prefab == null)
        {
            LogFail("Load Prefab", prefabLoadPath + " (asset null)");
            return;
        }

        Vector3 pos = GetSpawnPosition();
        prefabInstance = Instantiate(prefab, pos, Quaternion.identity, spawnRoot);
        targetRenderer = FindTargetRenderer();

        if (targetRenderer == null)
        {
            LogFail("Load Prefab", "no Renderer on instance");
            return;
        }

        LogOk("Load Prefab", prefabLoadPath + " -> " + targetRenderer.name + " at " + pos);
    }

    void TestApplySpriteToMaterial()
    {
        if (!EnsurePrefabInstanceReady(out Renderer renderer))
            return;

        spriteRes?.Release();
        spriteRes = BundleResLoader.Instance.Load<Sprite>(spriteLoadPath);
        Sprite sprite = spriteRes?.GetAsset<Sprite>();
        if (sprite == null)
        {
            LogFail("Apply Sprite", spriteLoadPath);
            return;
        }

        Material mat = renderer.material;
        if (!ApplyTexture(mat, sprite.texture))
        {
            LogFail("Apply Sprite", "material has no usable texture property");
            return;
        }

        LogOk("Apply Sprite", spriteLoadPath + " -> " + renderer.name + ".material");
    }

    void TestReplaceMaterial()
    {
        if (!EnsurePrefabInstanceReady(out Renderer renderer))
            return;

        materialRes?.Release();
        materialRes = BundleResLoader.Instance.Load<Material>(materialLoadPath);
        Material loadedMat = materialRes?.GetAsset<Material>();
        if (loadedMat == null)
        {
            LogFail("Replace Material", materialLoadPath);
            return;
        }

        renderer.material = loadedMat;
        LogOk("Replace Material", materialLoadPath + " -> " + renderer.name);
    }

    void TestLoadCacheHit()
    {
        AbstractResource first = BundleResLoader.Instance.Load<Sprite>(spriteLoadPath);
        AbstractResource second = BundleResLoader.Instance.Load<Sprite>(spriteLoadPath);

        if (first == null || second == null)
        {
            LogFail("Load cache", spriteLoadPath);
            return;
        }

        if (first != second)
        {
            LogFail("Load cache", "expected same AbstractResource");
            return;
        }

        if (first.GetAsset<Sprite>() == null)
        {
            LogFail("Load cache", "sprite null");
            return;
        }

        LogOk("Load cache", "same instance OK");
        second.Release();
    }

    void TestLoadByAssetPathAndBundle()
    {
        AbstractResource byPath = BundleResLoader.Instance.LoadByAssetPath<Sprite>(spriteAssetPath);
        if (byPath == null || byPath.GetAsset<Sprite>() == null)
        {
            LogFail("LoadByAssetPath", spriteAssetPath);
            return;
        }

        byPath.Release();

        AbstractResource byBundle = BundleResLoader.Instance.LoadByBundle<Sprite>(spriteBundleName, spriteAssetName);
        if (byBundle == null || byBundle.GetAsset<Sprite>() == null)
        {
            LogFail("LoadByBundle", spriteBundleName + "/" + spriteAssetName);
            return;
        }

        byBundle.Release();
        LogOk("LoadByAssetPath+Bundle", "aux API OK");
    }

    void TestLoadInvalidPath()
    {
        AbstractResource res = BundleResLoader.Instance.Load<Sprite>(invalidLoadPath);
        if (res == null)
            LogOk("Load invalid", invalidLoadPath + " (expected null)");
        else
        {
            LogFail("Load invalid", "should return null");
            res.Release();
        }
    }

    void TestReleaseReplacedResources()
    {
        if (spriteRes != null)
        {
            spriteRes.Release();
            spriteRes = null;
        }

        if (materialRes != null)
        {
            materialRes.Release();
            materialRes = null;
        }

        LogOk("Release replaced", "sprite + material released");
    }

    void TestDestroyPrefabInstance()
    {
        CleanupPrefabInstance();
        targetRenderer = null;

        if (prefabRes != null)
        {
            prefabRes.Release();
            prefabRes = null;
        }

        LogOk("Destroy Prefab", "instance destroyed, prefab resource released");
    }

    void TestFinishAndUnloadAll()
    {
        CleanupScenario();
        BundleResLoader.Instance.UnloadAll();
        LogOk("Finish", "UnloadAll done, scenario complete");
    }

    bool EnsurePrefabInstanceReady(out Renderer renderer)
    {
        renderer = FindTargetRenderer();
        if (prefabInstance != null && renderer != null)
            return true;

        LogFail("Scenario", "prefab instance or renderer missing; run Case 1 first");
        return false;
    }

    Renderer FindTargetRenderer()
    {
        if (prefabInstance == null)
            return targetRenderer;

        return prefabInstance.GetComponentInChildren<Renderer>(true);
    }

    bool ApplyTexture(Material mat, Texture texture)
    {
        if (mat == null || texture == null)
            return false;

        if (string.IsNullOrEmpty(textureShaderProperty) || textureShaderProperty == "_MainTex")
        {
            mat.mainTexture = texture;
            return mat.mainTexture == texture;
        }

        if (!mat.HasProperty(textureShaderProperty))
            return false;

        mat.SetTexture(textureShaderProperty, texture);
        return mat.GetTexture(textureShaderProperty) == texture;
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 basePos = spawnRoot != null ? spawnRoot.position : Vector3.zero;
        return basePos + Vector3.right * (caseIndex * spawnOffsetX);
    }

    void CleanupPrefabInstance()
    {
        if (prefabInstance == null)
            return;

        Destroy(prefabInstance);
        prefabInstance = null;
    }

    void CleanupScenario()
    {
        CleanupPrefabInstance();
        targetRenderer = null;

        spriteRes?.Release();
        spriteRes = null;

        materialRes?.Release();
        materialRes = null;

        prefabRes?.Release();
        prefabRes = null;
    }

    void LogOk(string api, string detail)
    {
        Debug.Log(LogPrefix + " OK | " + api + " | " + detail);
        logCollector?.Record(currentCaseId, caseIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError(LogPrefix + " FAIL | " + api + " | " + detail);
        logCollector?.Record(currentCaseId, caseIndex, api, false, detail);
    }
}
