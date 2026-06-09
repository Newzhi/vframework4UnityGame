using UnityEngine;

/// <summary>
/// 手动 Smoke 测试：挂到场景物体，Play 后按 Inspector 配置加载资源。
/// 对应用例 L-024 / L-033；需先 DeviceDebug 打包并 Init 对应 bundleRoot。
/// </summary>
public class ABLoadSmokeTest : MonoBehaviour
{
    [Header("Bundle 根目录（空=StreamingAssets；可填首包 base，不含平台子目录）")]
    public string bundleRootPath;

    [Tooltip("与 BuildSetting.usePlatformSubfolders 一致，自动追加 StandaloneWindows64 等")]
    public bool usePlatformSubfolder = true;

    [Header("简路径 Load（默认 API），如 Atlas/Role/Hog_Attack_000")]
    public string loadPath;

    [Header("Unity 完整路径（可选），如 Assets/AssetBundle/...")]
    public string assetPath;

    [Header("按 bundle+asset 桥接加载（辅助）")]
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
        string rootArg = string.IsNullOrEmpty(bundleRootPath) ? null : bundleRootPath;

        if (!loader.Init(rootArg, usePlatformSubfolder))
        {
            Debug.LogError("[ABLoadSmokeTest] Init failed");
            return;
        }

        Debug.Log("[ABLoadSmokeTest] Catalogue loaded, entries=" +
            (loader.GetCatalogue().Catalog.entries?.Length ?? 0));

        if (!string.IsNullOrEmpty(loadPath))
        {
            AbstractResource res = loader.Load<Sprite>(loadPath);
            LogResult("Load", loadPath, res);
        }

        if (!string.IsNullOrEmpty(assetPath))
        {
            AbstractResource byAssetPath = loader.LoadByAssetPath<Sprite>(assetPath);
            LogResult("LoadByAssetPath", assetPath, byAssetPath);
        }

        if (!string.IsNullOrEmpty(bundleName) && !string.IsNullOrEmpty(assetName))
        {
            AbstractResource byBundle = loader.LoadByBundle<Sprite>(bundleName, assetName);
            LogResult("LoadByBundle", bundleName + "/" + assetName, byBundle);
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
