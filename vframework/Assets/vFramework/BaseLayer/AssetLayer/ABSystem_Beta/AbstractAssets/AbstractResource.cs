using System;
using System.IO;
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
    internal void LoadAsset(Type assetType, string fallbackAssetPath = null)
    {
        if (assetType == null)
            assetType = typeof(Object);

        AssetBundle bundle = BundleManager.AcquireBundleWithDependencies(bundleName);
        if (bundle == null)
            return;

        asset = TryLoadFromBundle(bundle, assetName, assetType, fallbackAssetPath);

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
        return InstantiateAt(Vector3.zero, Quaternion.identity, null);
    }

    public GameObject InstantiateAt(Vector3 worldPosition, Quaternion worldRotation, Transform parent)
    {
        if (!(asset is GameObject prefab))
        {
            Debug.LogError("Asset is not GameObject, key:" + assetKey);
            return null;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.transform.SetPositionAndRotation(worldPosition, worldRotation);
        if (parent != null)
            instance.transform.SetParent(parent, true);

        return instance;
    }

    static Object TryLoadFromBundle(AssetBundle bundle, string assetName, Type assetType, string fallbackAssetPath)
    {
        if (bundle == null)
            return null;

        if (!string.IsNullOrEmpty(assetName))
        {
            Object loaded = bundle.LoadAsset(assetName, assetType);
            if (loaded != null)
                return loaded;
        }

        if (string.IsNullOrEmpty(fallbackAssetPath))
            return null;

        Object byPath = bundle.LoadAsset(fallbackAssetPath, assetType);
        if (byPath != null)
            return byPath;

        string fileName = Path.GetFileName(fallbackAssetPath);
        if (!string.IsNullOrEmpty(fileName) && fileName != assetName && fileName != fallbackAssetPath)
        {
            byPath = bundle.LoadAsset(fileName, assetType);
            if (byPath != null)
                return byPath;
        }

        string nameNoExt = Path.GetFileNameWithoutExtension(fallbackAssetPath);
        if (!string.IsNullOrEmpty(nameNoExt) && nameNoExt != assetName)
        {
            byPath = bundle.LoadAsset(nameNoExt, assetType);
            if (byPath != null)
                return byPath;
        }

        return bundle.LoadAsset(fallbackAssetPath);
    }

    #endregion
}
