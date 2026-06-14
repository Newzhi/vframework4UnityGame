using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
    readonly Dictionary<string, PrefabPool> poolsByLoadPath = new Dictionary<string, PrefabPool>();

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

            if (resourceDic.Count > 0)
            {
                foreach (AbstractResource res in resourceDic.Values)
                {
                    if (res == null)
                        continue;

                    res.onUnLoad = null;
                    res.UnLoad();
                }
            }
            resourceDic.Clear();

            bool catalogueLoaded = catalogue.LoadFromBundleRoot(bundleRootPath);
#if UNITY_EDITOR
            if (!catalogueLoaded)
                catalogueLoaded = catalogue.LoadFromProjectCatalogue();
#endif

            if (!catalogueLoaded)
            {
                initialized = false;
                Debug.LogError("BundleResLoader Init failed: catalogue not loaded from " + bundleRootPath);
                return false;
            }

            DefaultBundlePathResolver resolver = DefaultBundlePathResolver.Create(bundleRootPath);
            BundleManager.Init(bundleRootPath, catalogue);
            AssetRouter.Instance.Init(catalogue, resolver);

            if (catalogue.Catalog == null || catalogue.Catalog.bundles == null || catalogue.Catalog.bundles.Length == 0)
            {
                Debug.LogWarning("Catalogue loaded but bundle dependency map is empty. Cross-bundle dependencies will not be preloaded (EditorTest may produce this).");
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

    //TODO 用于业务侧预先加载对应模块；见 Docs/BusinessApiAndCdnPlanning.md §1 需求4
    public IAssetHandle PreLoad<T>()
    {
        return null;
    }

    /// <summary>
    /// 同步加载。loadPath 为相对打包根目录的简路径，无扩展名。
    /// 例：Default 规则下 targetDirectory=Assets/AssetBundle → Load&lt;Sprite&gt;("Atlas/Role/Hog_Attack_000")
    /// </summary>
    public IAssetHandle Load<T>(string loadPath) where T : Object
    {
        if (string.IsNullOrEmpty(loadPath))
        {
            Debug.LogError("Load path is null or empty.");
            return null;
        }

        if (!EnsureInitialized())
        {
            Debug.LogError("BundleResLoader not initialized; cannot load: " + loadPath);
            return null;
        }

        if (ResourcesAssetProvider.IsResourcesLoadPath(loadPath))
            return LoadResources<T>(loadPath);

        if (!catalogue.TryGetEntryByLoadPath(loadPath, out AssetCatalogEntry entry))
        {
            Debug.LogError("Load path not found in catalogue: " + loadPath);
            return null;
        }

        return LoadByBundle<T>(entry.bundleName, entry.assetName, entry.assetPath, loadPath);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loadPath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IAssetHandle LoadResources<T>(string loadPath) where T : Object
    {
        if (resourceDic.TryGetValue(loadPath, out AbstractResource res))
        {
            res.AddReference();
            if (res.GetAsset<T>() == null)
            {
                res.Release();
                Debug.LogError("LoadResources type mismatch for: " + loadPath + ", requested type: " + typeof(T).Name);
                return null;
            }
            return res;
        }

        res = new AbstractResource(loadPath, null, null, null, loadPath);
        res.onUnLoad = () => resourceDic.Remove(loadPath);
        resourceDic.Add(loadPath, res);
        res.AddReference();
        res.LoadAsset(typeof(T), null, loadPath);

        if (res.GetAsset<T>() == null)
        {
            res.Release();
            return null;
        }

        return res;
    }
    
    /// <summary>
    /// 业务侧需要加载三四个但是用池会比较浪费的情况实用这个方法将句柄交给创建的对象来管理
    /// </summary>
    /// <param name="loadPath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IAssetHandle LoadWithAutoUnLoad<T>(string loadPath) where T : Object
    {
        //TODO 待实现
       return Load<T>(loadPath);
    }
    
    /// <summary>
    /// 业务侧需要加载三四个但是用池会比较浪费的情况实用这个方法将句柄交给创建的对象来管理（异步版本）
    /// </summary>
    /// <param name="loadPath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IAssetHandle LoadUniTaskAsynWithAutoUnLoad<T>(string loadPath) where T : Object
    {
        //TODO 待实现
        return Load<T>(loadPath);
    }

    /// <summary>
    /// UniTask 异步加载默认入口。当前阶段先提供 await 形态；
    /// 实际资源 I/O 仍复用同步 Load，后续接入 CDN 下载/并发合并。
    /// </summary>
    public async UniTask<IAssetHandle> LoadUniTaskAsync<T>(string loadPath) where T : Object
    {
        // 让调用方可 await，避免在同一调用栈内立即阻塞。
        await UniTask.Yield(PlayerLoopTiming.Update);

        return Load<T>(loadPath);
    }

    /// <summary>
    /// UniTask 带回调加载，默认走 UniTask 异步；useUniTask=false 时走同步 Load 并立即回调。
    /// </summary>
    public void LoadUniTaskWithCallback<T>(string loadPath, Action<IAssetHandle> onComplete, Action<string> onFailed = null, bool useUniTask = true) where T : Object
    {
        if (!useUniTask)
        {
            InvokeSyncLoadWithCallback(
                () => Load<T>(loadPath),
                onComplete,
                onFailed,
                "LoadUniTaskWithCallback failed, loadPath=" + loadPath);
            return;
        }

        InvokeUniTaskLoadWithCallback(
            () => LoadUniTaskAsync<T>(loadPath),
            onComplete,
            onFailed,
            "LoadUniTaskWithCallback failed, loadPath=" + loadPath);
    }

    /// <summary>
    /// 按 Unity 完整 assetPath 的 UniTask 回调加载，默认走 UniTask 异步。
    /// </summary>
    public void LoadByAssetPathUniTaskWithCallback<T>(string assetPath, Action<IAssetHandle> onComplete, Action<string> onFailed = null, bool useUniTask = true) where T : Object
    {
        if (!useUniTask)
        {
            InvokeSyncLoadWithCallback(
                () => LoadByAssetPath<T>(assetPath),
                onComplete,
                onFailed,
                "LoadByAssetPathUniTaskWithCallback failed, assetPath=" + assetPath);
            return;
        }

        InvokeUniTaskLoadWithCallback(
            () => LoadByAssetPathUniTaskAsync<T>(assetPath),
            onComplete,
            onFailed,
            "LoadByAssetPathUniTaskWithCallback failed, assetPath=" + assetPath);
    }

    /// <summary>
    /// 按 bundle+asset 的 UniTask 回调加载，默认走 UniTask 异步。
    /// </summary>
    public void LoadByBundleUniTaskWithCallback<T>(string bundleName, string assetName, Action<IAssetHandle> onComplete, Action<string> onFailed = null, bool useUniTask = true, string assetPath = null) where T : Object
    {
        if (!useUniTask)
        {
            InvokeSyncLoadWithCallback(
                () => LoadByBundle<T>(bundleName, assetName, assetPath),
                onComplete,
                onFailed,
                "LoadByBundleUniTaskWithCallback failed, key=" + bundleName + "/" + assetName);
            return;
        }

        InvokeUniTaskLoadWithCallback(
            async () => await LoadByBundleUniTaskAsync<T>(bundleName, assetName, assetPath),
            onComplete,
            onFailed,
            "LoadByBundleUniTaskWithCallback failed, key=" + bundleName + "/" + assetName);
    }

    async UniTask<IAssetHandle> LoadByAssetPathUniTaskAsync<T>(string assetPath) where T : Object
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        return LoadByAssetPath<T>(assetPath);
    }

    async UniTask<IAssetHandle> LoadByBundleUniTaskAsync<T>(string bundleName, string assetName, string assetPath = null) where T : Object
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        return LoadByBundle<T>(bundleName, assetName, assetPath);
    }

    /// <summary>
    /// 卸载资源：可选直接销毁实例，并减少资源引用计数。
    /// </summary>
    /// <param name="resource">由 Load/LoadUniTaskAsync 返回的资源句柄，可为 null。</param>
    /// <param name="instance">业务侧实例对象，可为 null；不为 null 时会直接 Destroy。</param>
    /// <param name="onComplete">卸载完成回调，参数表示是否执行了至少一个有效卸载动作。</param>
    public void Unload(IAssetHandle resource, GameObject instance = null, Action<bool> onComplete = null)
    {
        bool unloaded = false;

        if (instance != null)
        {
            Object.Destroy(instance);
            unloaded = true;
        }

        if (resource != null)
        {
            resource.Release();
            unloaded = true;
        }

        if (!unloaded)
            Debug.LogWarning("Unload called with null resource and null instance.");

        onComplete?.Invoke(unloaded);
    }

    //卸载全部资源
    public void UnloadAll()
    {
        DestroyAllPools();

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

    #region 对象池
    
    /* ------------------------------------------------------------------------
     * 对象池用来解决一个业务需要大量加载某个预制体的情况
     * 创建池的时候会移交Handle处理权，由池来负责最后句柄的回收，保证引用计数安全
     * 创建池子的时候会检查是否已经有对应池子了，如果有的话则扩容复用已有的池子，池子会变成共享池
     * 理论上是谁创建的池子谁负责回收池子，保证引用计数安全，符合RAII原则
     * 业务场景举例：
     * 比如一个敌人创建的池子，那么在他死亡的时候就需要销毁
     * 如果这个池子是个共享池，那么池可能也需要维护一个引用计数，计数到0的时候才能被销毁
     ------------------------------------------------------------------------- */
    
    #region CreatPool

    /// <summary>
    /// Load Prefab 并创建 <see cref="PrefabPool"/>；句柄所有权移交池，业务勿再 Release，由 <see cref="PrefabPool.DestroyPool"/> 统一回收。
    /// </summary>
    public PrefabPool CreatPool(string loadPath, Transform inactiveRoot = null, int maxInactiveCapacity = 0)
    {
        if (string.IsNullOrEmpty(loadPath))
        {
            Debug.LogError("CreatPool: loadPath is null or empty.");
            return null;
        }

        IAssetHandle handle = Load<GameObject>(loadPath);
        if (handle == null)
        {
            Debug.LogError("CreatPool: Load failed, path=" + loadPath);
            return null;
        }

        PrefabPool pool = new PrefabPool(handle, inactiveRoot, maxInactiveCapacity);
        pool.CreatPool();
        if (!pool.IsPoolCreated)
        {
            Debug.LogError("CreatPool: PrefabPool.CreatPool failed, path=" + loadPath);
            handle.Release();
            return null;
        }

        return pool;
    }

    /// <summary>
    /// 按 loadPath 去重创建池：多脚本调用同一路径时共享同一 <see cref="PrefabPool"/> 与闲置根节点。
    /// inactiveRoot 仅在首次创建时生效；已存在池时忽略该参数。
    /// </summary>
    public PrefabPool GetOrCreatPool(string loadPath, Transform inactiveRoot = null, int maxInactiveCapacity = 0)
    {
        if (string.IsNullOrEmpty(loadPath))
        {
            Debug.LogError("GetOrCreatPool: loadPath is null or empty.");
            return null;
        }

        if (poolsByLoadPath.TryGetValue(loadPath, out PrefabPool existing) && existing != null && existing.IsPoolCreated)
            return existing;

        Transform root = inactiveRoot ?? PoolSceneRoots.GetOrCreateInactiveRoot(loadPath);
        PrefabPool pool = CreatPool(loadPath, root, maxInactiveCapacity);
        if (pool != null && pool.IsPoolCreated)
            poolsByLoadPath[loadPath] = pool;

        return pool;
    }

    /// <summary>活跃实例逻辑父节点，命名 Active_{logicalName}，挂在 <see cref="PoolSceneRoots.RuntimeRootName"/> 下。</summary>
    public Transform GetOrCreateActivePoolRoot(string logicalName)
    {
        return PoolSceneRoots.GetOrCreateActiveRoot(logicalName);
    }

    public bool TryGetPool(string loadPath, out PrefabPool pool)
    {
        if (string.IsNullOrEmpty(loadPath))
        {
            pool = null;
            return false;
        }

        return poolsByLoadPath.TryGetValue(loadPath, out pool) && pool != null && pool.IsPoolCreated;
    }
    #endregion
    
    #region 销毁池

    /// <summary>销毁已注册池并移除去重表项；活跃实例须已全部 Release。</summary>
    public bool DestroyPoolByLoadPath(string loadPath)
    {
        if (string.IsNullOrEmpty(loadPath))
            return false;

        if (!poolsByLoadPath.TryGetValue(loadPath, out PrefabPool pool) || pool == null)
            return false;

        if (!pool.CanDestroyPool)
        {
            Debug.LogWarning("DestroyPoolByLoadPath: pool still has active instances, path=" + loadPath);
            return false;
        }

        pool.DestroyPool();
        poolsByLoadPath.Remove(loadPath);
        return true;
    }

    public void DestroyAllPools()
    {
        foreach (KeyValuePair<string, PrefabPool> pair in poolsByLoadPath)
        {
            if (pair.Value == null)
                continue;

            if (!pair.Value.CanDestroyPool)
                Debug.LogWarning("DestroyAllPools: skipping pool with active instances, path=" + pair.Key);

            pair.Value.DestroyPool();
        }

        poolsByLoadPath.Clear();
        PoolSceneRoots.ClearCache();
    }

    #endregion

    #endregion

    #region 辅助函数

    /// <summary>按 bundle 名 + 包内 asset 名加载，Resource 层与 BundleManager 的桥接。</summary>
    public IAssetHandle LoadByBundle<T>(string bundleName, string assetName, string assetPath = null, string loadPath = null) where T : Object
    {
        if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("LoadByBundle failed, bundleName or assetName is null/empty.");
            return null;
        }

        if (!EnsureInitialized())
        {
            Debug.LogError("BundleResLoader not initialized; cannot load bundle: " + bundleName + "/" + assetName);
            return null;
        }

        string key = bundleName + "/" + assetName;

        if (resourceDic.TryGetValue(key, out AbstractResource res))
        {
            res.AddReference();
            if (res.GetAsset<T>() == null)
            {
                // 命中缓存但泛型不匹配时回滚本次引用，避免悬挂引用。
                res.Release();
                Debug.LogError("LoadByBundle type mismatch for cached resource: " + key + ", requested type: " + typeof(T).Name);
                return null;
            }
            return res;
        }

        res = new AbstractResource(key, bundleName, assetName, assetPath, loadPath);
        res.onUnLoad = () => resourceDic.Remove(key);
        resourceDic.Add(key, res);
        res.AddReference();
        res.LoadAsset(typeof(T), assetPath, loadPath);

        if (res.GetAsset<T>() == null)
        {
            res.Release();
            return null;
        }

        return res;
    }

    /// <summary>按 Unity 工程完整 assetPath 加载，如 Assets/AssetBundle/Atlas/Role/Hog.png</summary>
    public IAssetHandle LoadByAssetPath<T>(string assetPath) where T : Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Asset path is null or empty.");
            return null;
        }

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

        return LoadByBundle<T>(
            entry.bundleName,
            entry.assetName,
            entry.assetPath,
            CatalogueReader.ToLoadPath(entry.assetPath, catalogue.Catalog?.resourceRoot));
    }

    void InvokeSyncLoadWithCallback(Func<IAssetHandle> loader, Action<IAssetHandle> onComplete, Action<string> onFailed, string failMessage)
    {
        try
        {
            IAssetHandle handle = loader.Invoke();
            if (handle != null)
            {
                onComplete?.Invoke(handle);
                return;
            }

            if (onFailed != null)
                onFailed.Invoke(failMessage);
            else
                Debug.LogError(failMessage);
        }
        catch (Exception ex)
        {
            string msg = failMessage + ", exception=" + ex.Message;
            if (onFailed != null)
                onFailed.Invoke(msg);
            else
                Debug.LogError(msg);
        }
    }

    void InvokeUniTaskLoadWithCallback(Func<UniTask<IAssetHandle>> loader, Action<IAssetHandle> onComplete, Action<string> onFailed, string failMessage)
    {
        UniTask.Void(async () =>
        {
            try
            {
                IAssetHandle handle = await loader.Invoke();
                if (handle != null)
                {
                    onComplete?.Invoke(handle);
                    return;
                }

                if (onFailed != null)
                    onFailed.Invoke(failMessage);
                else
                    Debug.LogError(failMessage);
            }
            catch (Exception ex)
            {
                string msg = failMessage + ", exception=" + ex.Message;
                if (onFailed != null)
                    onFailed.Invoke(msg);
                else
                    Debug.LogError(msg);
            }
        });
    }

    #endregion

}

