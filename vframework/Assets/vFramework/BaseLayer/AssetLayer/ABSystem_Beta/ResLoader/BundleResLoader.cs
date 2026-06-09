using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

//从AssetBundle加载抽象资源，管理Resource层缓存与引用
public class BundleResLoader
{
    #region 变量定义

    private readonly CatalogueReader catalogue = new CatalogueReader();
    private Dictionary<string, AbstractResource> resourceDic = new Dictionary<string, AbstractResource>();

    #endregion

    #region 初始化

    public bool Init(string bundleRootPath)
    {
        if (string.IsNullOrEmpty(bundleRootPath))
            bundleRootPath = Application.streamingAssetsPath;

        BundleManager.Init(bundleRootPath, catalogue);
        resourceDic.Clear();

        if (!catalogue.LoadFromBundleRoot(bundleRootPath))
        {
            Debug.LogError("BundleResLoader Init failed: catalogue not loaded from " + bundleRootPath);
            return false;
        }

        return true;
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

    //同步加载，返回缓存资源；用完后需调用 resource.Release()
    public AbstractResource Load<T>(string bundleName, string assetName) where T : Object
    {
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
        res.LoadAsset();

        if (res.GetAsset<Object>() == null)
        {
            resourceDic.Remove(key);
            return null;
        }

        return res;
    }

    public AbstractResource LoadByPath<T>(string assetPath) where T : Object
    {
        if (!catalogue.TryGetEntry(assetPath, out AssetCatalogEntry entry))
        {
            Debug.LogError("Asset path not found in catalogue: " + assetPath);
            return null;
        }

        return Load<T>(entry.bundleName, entry.assetName);
    }

    //TODO 异步加载，期望 UniTask；设计基线默认 API，见 Docs/业务API与CDN规划.md §1 需求2
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

    #endregion

}
