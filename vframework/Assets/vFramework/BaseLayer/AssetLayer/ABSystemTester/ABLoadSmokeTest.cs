using UnityEngine;

/// <summary>
/// 手动 Smoke 测试：挂到场景物体，Play 后按 Inspector 配置加载资源。
/// 对应用例 L-024 / L-033；需先 DeviceDebug 打包并 Init 对应 bundleRoot。
/// </summary>
public class ABLoadSmokeTest : MonoBehaviour
{
    [Header("Bundle 根目录（空则 StreamingAssets）")]
    public string bundleRootPath;

    [Header("按路径加载（L-033）")]
    public string assetPath;

    [Header("按 bundle+asset 加载（对照 L-001）")]
    public string bundleName;
    public string assetName;

    [Header("Play 时自动执行")]
    public bool runOnStart = true;

    [Header("加载后 Instantiate Prefab")]
    public bool instantiatePrefab = true;

    BundleResLoader loader;

    void Start()
    {
        if (runOnStart)
            RunSmokeTest();
    }

    [ContextMenu("Run Smoke Test")]
    public void RunSmokeTest()
    {
        loader = new BundleResLoader();
        string root = string.IsNullOrEmpty(bundleRootPath)
            ? Application.streamingAssetsPath
            : bundleRootPath;

        if (!loader.Init(root))
        {
            Debug.LogError("[ABLoadSmokeTest] Init failed");
            return;
        }

        Debug.Log("[ABLoadSmokeTest] Catalogue loaded, entries=" +
            (loader.GetCatalogue().Catalog.entries?.Length ?? 0));

        if (!string.IsNullOrEmpty(assetPath))
        {
            AbstractResource byPath = loader.LoadByPath<GameObject>(assetPath);
            LogResult("LoadByPath", assetPath, byPath);
            if (byPath != null && instantiatePrefab)
                byPath.Instantiate();
        }

        if (!string.IsNullOrEmpty(bundleName) && !string.IsNullOrEmpty(assetName))
        {
            AbstractResource byName = loader.Load<GameObject>(bundleName, assetName);
            LogResult("Load", bundleName + "/" + assetName, byName);
        }
    }

    [ContextMenu("Release All")]
    public void ReleaseAll()
    {
        loader?.UnloadAll();
        loader = null;
        Debug.Log("[ABLoadSmokeTest] UnloadAll done");
    }

    static void LogResult(string method, string id, AbstractResource res)
    {
        if (res == null)
            Debug.LogError("[ABLoadSmokeTest] " + method + " failed: " + id);
        else
            Debug.Log("[ABLoadSmokeTest] " + method + " OK: " + id);
    }
}
