using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

//从AssetBundle加载抽象资源，管理Resource层缓存与引用
public class BundleResLoader
{
    #region 单例

    static volatile BundleResLoader instance;
    static readonly object instanceLock = new object();

    public static BundleResLoader Instance
    {
        get
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                        instance = new BundleResLoader();
                }
            }
            return instance;
        }
    }

    #endregion

    #region 变量定义

    const bool DefaultUsePlatformSubfolder = true;

    readonly object initLock = new object();
    bool initialized;

    readonly CatalogueReader catalogue = new CatalogueReader();
    Dictionary<string, AbstractResource> resourceDic = new Dictionary<string, AbstractResource>();

    #endregion

    #region 初始化

    public bool Init(string bundleRootPath, bool usePlatformSubfolder = true)
    {
        lock (initLock)
        {
            if (initialized && catalogue.IsLoaded)
            {
                Debug.LogWarning("BundleResLoader already initialized; ignoring repeated Init.");
                return true;
            }

            bundleRootPath = BundlePlatformPaths.ResolveRuntimeBundleRoot(bundleRootPath, usePlatformSubfolder);

            BundleManager.Init(bundleRootPath, catalogue);
            resourceDic.Clear();

            if (!catalogue.LoadFromBundleRoot(bundleRootPath))
            {
                initialized = false;
                Debug.LogError("BundleResLoader Init failed: catalogue not loaded from " + bundleRootPath);
                return false;
            }

            initialized = true;
            return true;
        }
    }

    bool EnsureInitialized()
    {
        if (initialized && catalogue.IsLoaded)
            return true;

        lock (initLock)
        {
            if (initialized && catalogue.IsLoaded)
                return true;

            return Init(null, DefaultUsePlatformSubfolder);
        }
    }

    /// <summary>懒 Init 预热：加载 Catalogue 与 Bundle 根目录，供启动或测试在首次 Load 前调用。</summary>
    public bool EnsureReady()
    {
        return EnsureInitialized();
    }

    /// <summary>运行时默认首包根目录（StreamingAssets + 当前平台子目录）。</summary>
    public static string GetDefaultRuntimeBundleRoot(bool usePlatformSubfolder = true)
    {
        return BundlePlatformPaths.ResolveRuntimeBundleRoot(null, usePlatformSubfolder);
    }

    public CatalogueReader GetCatalogue()
    {
        return catalogue;
    }

    public bool IsCatalogueLoaded => catalogue.IsLoaded;

    #endregion

    #region 加载/卸载

    //TODO 用于业务侧预先加载对应模块；见 Docs/业务API与CDN规划.md §1 需求4
    T PreLoad<T>()
    {
        return default(T);
    }

    /// <summary>
    /// 同步加载。loadPath 为相对打包根目录的简路径，无扩展名。
    /// 例：Default 规则下 targetDirectory=Assets/AssetBundle → Load&lt;Sprite&gt;("Atlas/Role/Hog_Attack_000")
    /// </summary>
    public AbstractResource Load<T>(string loadPath) where T : Object
    {
        if (!EnsureInitialized())
        {
            Debug.LogError("BundleResLoader not initialized; cannot load: " + loadPath);
            return null;
        }

        if (!catalogue.TryGetEntryByLoadPath(loadPath, out AssetCatalogEntry entry))
        {
            Debug.LogError("Load path not found in catalogue: " + loadPath);
            return null;
        }

        return LoadByBundle<T>(entry.bundleName, entry.assetName, entry.assetPath);
    }

    //TODO 异步加载(默认API)，期望 UniTask；设计基线默认 API，见 Docs/业务API与CDN规划.md §1 需求2
    T LoadAsync<T>()
    {
        T t = default;
        return t;
    }

    //带有回调函数的加载，默认异步；见 Docs/业务API与CDN规划.md §1 需求3
    void LoadWithCallback<T>()
    {

    }

    //卸载资源
    void Unload()
    {

    }

    //卸载全部资源
    public void UnloadAll()
    {
        AbstractResource[] resources = new AbstractResource[resourceDic.Count];
        resourceDic.Values.CopyTo(resources, 0);
        resourceDic.Clear();

        foreach (AbstractResource res in resources)
        {
            res.onUnLoad = null;
            res.UnLoad();
        }

        BundleManager.UnloadAll();
    }

    #endregion

    #region 辅助函数

    /// <summary>按 bundle 名 + 包内 asset 名加载，Resource 层与 BundleManager 的桥接。</summary>
    public AbstractResource LoadByBundle<T>(string bundleName, string assetName, string assetPath = null) where T : Object
    {
        if (!EnsureInitialized())
        {
            Debug.LogError("BundleResLoader not initialized; cannot load bundle: " + bundleName + "/" + assetName);
            return null;
        }

        string key = bundleName + "/" + assetName;

        if (resourceDic.TryGetValue(key, out AbstractResource res))
        {
            res.AddReference();
            return res;
        }

        res = new AbstractResource(key, bundleName, assetName);
        res.onUnLoad = () => resourceDic.Remove(key);
        resourceDic.Add(key, res);
        res.AddReference();
        res.LoadAsset(typeof(T), assetPath);

        if (res.GetAsset<T>() == null)
        {
            resourceDic.Remove(key);
            return null;
        }

        return res;
    }

    /// <summary>按 Unity 工程完整 assetPath 加载，如 Assets/AssetBundle/Atlas/Role/Hog.png</summary>
    public AbstractResource LoadByAssetPath<T>(string assetPath) where T : Object
    {
        if (!EnsureInitialized())
        {
            Debug.LogError("BundleResLoader not initialized; cannot load asset path: " + assetPath);
            return null;
        }

        if (!catalogue.TryGetEntry(assetPath, out AssetCatalogEntry entry))
        {
            Debug.LogError("Asset path not found in catalogue: " + assetPath);
            return null;
        }

        return LoadByBundle<T>(entry.bundleName, entry.assetName, entry.assetPath);
    }

    #endregion

}

