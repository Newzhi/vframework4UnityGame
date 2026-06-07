using System;
using UnityEngine;
using Object = UnityEngine.Object;

//抽象资源：Resource层引用计数，Ref为0时释放Bundle引用
public class AbstractResource
{
    #region 变量定义

    private string assetKey;
    private string bundleName;
    private string assetName;
    private Object asset;
    private int Ref;
    internal Action onUnLoad;

    #endregion

    #region 构造

    internal AbstractResource(string assetKey, string bundleName, string assetName)
    {
        this.assetKey = assetKey;
        this.bundleName = bundleName;
        this.assetName = assetName;
    }

    #endregion

    #region 引用计数

    internal void AddReference()
    {
        Ref++;
    }

    internal void ReduceReference()
    {
        Ref--;
        if (Ref < 0)
        {
            Debug.LogError("AbstractResource ReduceReference less than 0, key:" + assetKey);
            Ref = 0;
        }
    }

    #endregion

    #region 加载/卸载

    //首次加载：AcquireBundle + LoadAsset
    internal void LoadAsset()
    {
        AssetBundle bundle = BundleManager.AcquireBundle(bundleName);
        if (bundle == null)
            return;

        asset = bundle.LoadAsset(assetName, typeof(Object));
        if (asset == null)
        {
            BundleManager.ReleaseBundle(bundleName);
            Debug.LogError("Asset load failed: " + assetName + " in " + bundleName);
        }
    }

    //Resource引用为0时调用，释放Bundle引用
    internal void UnLoad()
    {
        if (asset != null)
        {
            asset = null;
        }

        BundleManager.ReleaseBundle(bundleName);
        onUnLoad?.Invoke();
        onUnLoad = null;
    }

    //释放一次引用；Ref为0时自动UnLoad
    public void Release()
    {
        if (Ref <= 0)
            return;

        ReduceReference();
        if (Ref == 0)
            UnLoad();
    }

    #endregion

    #region 辅助函数

    public T GetAsset<T>() where T : Object
    {
        return asset as T;
    }

    //Prefab需业务侧自行Instantiate，实例Destroy与Release无关
    public GameObject Instantiate()
    {
        if (asset is GameObject prefab)
            return Object.Instantiate(prefab);

        Debug.LogError("Asset is not GameObject, key:" + assetKey);
        return null;
    }

    #endregion
}
