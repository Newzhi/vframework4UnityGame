using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Prefab 对象池：持有一个 <see cref="IAssetHandle"/>（一次 Load），复用 Instantiate 实例。
/// 通过 <see cref="BundleResLoader.CreatPool"/> 创建；句柄在 <see cref="DestroyPool"/> 时 Release。
/// </summary>
public sealed class PrefabPool
{
    #region 变量定义
    
    IAssetHandle prefabHandle; //句柄
    readonly Transform inactiveRoot;
    readonly Stack<GameObject> inactiveInstances = new Stack<GameObject>(32);
    readonly HashSet<GameObject> activeInstances = new HashSet<GameObject>();
    int maxInactiveCapacity;
    int refCount;//如果是别人也需要创建则引用计数+1，然后扩容
    bool isPoolCreated;

    public bool IsPoolCreated => isPoolCreated;
    public bool CanDestroyPool => isPoolCreated && activeInstances.Count == 0;
    public int ActiveCount => activeInstances.Count;
    public int InactiveCount => inactiveInstances.Count;

    /// <summary>闲置上限；0 表示不限制。超出时 Release 直接 Destroy 实例。</summary>
    public int MaxInactiveCapacity => maxInactiveCapacity;
    
    #endregion
   

    internal PrefabPool(IAssetHandle prefabHandle, Transform inactiveRoot = null, int maxInactiveCapacity = 0)
    {
        this.prefabHandle = prefabHandle;
        this.inactiveRoot = inactiveRoot;
        this.maxInactiveCapacity = maxInactiveCapacity;
    }
    
    #region 业务接口（创建池/销毁池）
    
    public void CreatPool()
    {
        if (isPoolCreated)
            return;

        if (prefabHandle == null)
        {
            Debug.LogError("PrefabPool.CreatPool: prefabHandle is null.");
            return;
        }

        if (prefabHandle.GetAsset<GameObject>() == null)
        {
            Debug.LogError("PrefabPool.CreatPool: handle is not a GameObject prefab.");
            return;
        }

        isPoolCreated = true;
    }

    public void DestroyPool()
    {
        if (!isPoolCreated && prefabHandle == null)
            return;

        DestroyAllInstances();

        if (prefabHandle != null)
        {
            prefabHandle.Release();
            prefabHandle = null;
        }

        isPoolCreated = false;
    }

    public GameObject GetObj()
    {
        return GetObj(Vector3.zero, Quaternion.identity, null);
    }

    public GameObject GetObj(Vector3 worldPosition, Quaternion worldRotation, Transform parent = null)
    {
        if (!isPoolCreated)
        {
            Debug.LogError("PrefabPool.GetObj: call CreatPool first.");
            return null;
        }

        GameObject instance = PopInactiveInstance();
        if (instance == null)
            instance = prefabHandle.InstantiateAt(worldPosition, worldRotation, parent);
        else
            ActivateInstance(instance, worldPosition, worldRotation, parent);

        if (instance == null)
            return null;

        activeInstances.Add(instance);
        return instance;
    }

    public void ReleaseObj(GameObject instance)
    {
        if (instance == null)
            return;

        if (!isPoolCreated)
        {
            Object.Destroy(instance);
            return;
        }

        if (!activeInstances.Remove(instance))
        {
#if UNITY_EDITOR
            Debug.LogWarning("PrefabPool.ReleaseObj: instance not from this pool.");
#endif
            return;
        }

        if (maxInactiveCapacity > 0 && inactiveInstances.Count >= maxInactiveCapacity)
        {
            Object.Destroy(instance);
            return;
        }

        DeactivateInstance(instance);
        inactiveInstances.Push(instance);
    }

    GameObject PopInactiveInstance()
    {
        while (inactiveInstances.Count > 0)
        {
            GameObject instance = inactiveInstances.Pop();
            if (instance != null)
                return instance;
        }

        return null;
    }

    static void ActivateInstance(GameObject instance, Vector3 worldPosition, Quaternion worldRotation, Transform parent)
    {
        Transform t = instance.transform;
        t.SetPositionAndRotation(worldPosition, worldRotation);
        if (parent != null)
            t.SetParent(parent, true);

        if (!instance.activeSelf)
            instance.SetActive(true);
    }

    void DeactivateInstance(GameObject instance)
    {
        if (inactiveRoot != null)
            instance.transform.SetParent(inactiveRoot, true);

        if (instance.activeSelf)
            instance.SetActive(false);
    }

    void DestroyAllInstances()
    {
        if (activeInstances.Count > 0)
        {
            foreach (GameObject instance in activeInstances)
            {
                if (instance != null)
                    Object.Destroy(instance);
            }

            activeInstances.Clear();
        }

        while (inactiveInstances.Count > 0)
        {
            GameObject instance = inactiveInstances.Pop();
            if (instance != null)
                Object.Destroy(instance);
        }
    }
    
    #endregion

    
}
