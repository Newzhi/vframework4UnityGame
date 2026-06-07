using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

//从AssetBundle加载抽象资源，管理Resource层缓存与引用
public class BundleResLoader
{
    #region 变量定义

    private Dictionary<string, AbstractResource> resourceDic = new Dictionary<string, AbstractResource>();

    #endregion

    #region 初始化

    public void Init(string bundleRootPath)
    {
        BundleManager.Init(bundleRootPath);
        resourceDic.Clear();
    }

    #endregion

    #region 加载/卸载

    //用于业务侧预先加载对应模块
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

    //异步加载
    T LoadAsync<T>()
    {
        T t = default;
        return t;
    }

    //带有回调函数的加载，默认异步
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
