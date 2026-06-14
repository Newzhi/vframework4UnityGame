using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Prefab 对象池：一次 Load，实例队列复用；单父节点 + SetActive，借还不 SetParent。
/// 懒增长：仅在 GetObj 缺实例时 Instantiate；refCount++ 只抬闲置上限，不预创建。
/// </summary>
public sealed class PrefabPool
{
    #region 变量定义

    IAssetHandle prefabHandle; //句柄：CreatPool 时 Load 一次，TearDown 时 Release 一次
    readonly Transform poolRoot; //池统一父节点（Pool_*），实例创建时挂一次，借还仅 SetActive
    readonly Queue<GameObject> inactiveInstances = new Queue<GameObject>(32); //闲置队列：ReleaseObj Enqueue，GetObj Dequeue
    readonly HashSet<GameObject> activeInstances = new HashSet<GameObject>(); //已借出实例，用于校验 ReleaseObj 与统计 ActiveCount
    readonly int baseInactiveCapacity; //单份持有者闲置上限（构造传入），共享时按 refCount 倍增
    int maxInactiveCapacity; //当前闲置上限 = baseInactiveCapacity × refCount（0 不限）
    int refCount; // GetOrCreatPool 次数；++ 时只更新闲置上限，不预 Instantiate
    bool isPoolCreated; //是否已完成 CreatPool，GetObj 前必须为 true

    /// <summary>池是否已创建（CreatPool 成功）。</summary>
    public bool IsPoolCreated => isPoolCreated;

    /// <summary>无借出实例时可安全 DestroyPool（不强制，TearDown 仍会销毁借出实例）。</summary>
    public bool CanDestroyPool => isPoolCreated && activeInstances.Count == 0;

    /// <summary>当前借出（GetObj 后未 ReleaseObj）数量。</summary>
    public int ActiveCount => activeInstances.Count;

    /// <summary>闲置队列中实例数量。</summary>
    public int InactiveCount => inactiveInstances.Count;

    /// <summary>共享池引用计数，与 CreatPool / DestroyPool 成对。</summary>
    public int RefCount => refCount;

    /// <summary>当前闲置上限；0 表示不限制。超出时 ReleaseObj 直接 Destroy 实例。</summary>
    public int MaxInactiveCapacity => maxInactiveCapacity;

    #endregion

    /// <summary>由 BundleResLoader.CreatPool 构造；句柄所有权移交本池。</summary>
    internal PrefabPool(IAssetHandle prefabHandle, Transform poolRoot = null, int maxInactiveCapacity = 0)
    {
        this.prefabHandle = prefabHandle;
        this.poolRoot = poolRoot;
        this.baseInactiveCapacity = maxInactiveCapacity;
        this.maxInactiveCapacity = maxInactiveCapacity;
    }

    #region 业务接口

    #region 创建池/销毁池

    /// <summary>首次 refCount=1；已创建则 refCount++ 并更新闲置上限（懒增长，不预创建实例）。</summary>
    public void CreatPool()
    {
        if (isPoolCreated)
        {
            refCount++;
            ApplyCapacityForRefCount();
            return;
        }

        if (prefabHandle == null || prefabHandle.GetAsset<GameObject>() == null)
        {
            Debug.LogError("PrefabPool.CreatPool: invalid prefab handle.");
            return;
        }

        isPoolCreated = true;
        refCount = 1;
        ApplyCapacityForRefCount();
    }

    /// <summary>refCount--；未归零则收缩上限；归零则 TearDown。</summary>
    public void DestroyPool()
    {
        if (!isPoolCreated)
            return;

        refCount--;
        if (refCount > 0)
        {
            ApplyCapacityForRefCount();
            TrimInactiveExcess();
            return;
        }

        TearDown();
    }

    /// <summary>UnloadAll 强制销毁，无视 refCount。</summary>
    internal void ForceDestroyPool()
    {
        if (!isPoolCreated)
            return;

        TearDown();
    }

    #endregion

    #region 取或者创建对象/回收对象

    /// <summary>借出实例，默认位姿与世界根。</summary>
    public GameObject GetObj()
    {
        return GetObj(Vector3.zero, Quaternion.identity, null);
    }

    /// <summary>
    /// 借出实例：Dequeue 复用或 InstantiateAt；设定位姿后 SetActive(true)。parent 参数已忽略（单父节点方案）。
    /// </summary>
    public GameObject GetObj(Vector3 worldPosition, Quaternion worldRotation, Transform parent = null)
    {
        if (!isPoolCreated)
        {
            Debug.LogError("PrefabPool.GetObj: call CreatPool first.");
            return null;
        }

        GameObject instance = null;
        while (inactiveInstances.Count > 0 && instance == null)
            instance = inactiveInstances.Dequeue();

        if (instance == null)
            instance = prefabHandle.InstantiateAt(worldPosition, worldRotation, poolRoot);
        else
        {
            Transform t = instance.transform;
            t.SetPositionAndRotation(worldPosition, worldRotation);
            if (!instance.activeSelf)
                instance.SetActive(true);
        }

        if (instance == null)
            return null;

        activeInstances.Add(instance);
        return instance;
    }

    /// <summary>归还实例：SetActive(false) 后 Enqueue；超上限则 Destroy。不 Release 句柄。</summary>
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
            return;

        if (maxInactiveCapacity > 0 && inactiveInstances.Count >= maxInactiveCapacity)
        {
            Object.Destroy(instance);
            return;
        }

        DeactivateInstance(instance);
        inactiveInstances.Enqueue(instance);
    }

    #endregion

    #endregion

    #region 辅助方法

    void ApplyCapacityForRefCount()
    {
        maxInactiveCapacity = baseInactiveCapacity > 0
            ? baseInactiveCapacity * refCount
            : 0;
    }

    void TrimInactiveExcess()
    {
        if (maxInactiveCapacity <= 0)
            return;

        while (inactiveInstances.Count > maxInactiveCapacity)
        {
            GameObject go = inactiveInstances.Dequeue();
            if (go != null)
                Object.Destroy(go);
        }
    }

    /// <summary>仅 SetActive(false)，不换父节点。</summary>
    void DeactivateInstance(GameObject instance)
    {
        if (instance.activeSelf)
            instance.SetActive(false);
    }

    void TearDown()
    {
        isPoolCreated = false;
        refCount = 0;
        maxInactiveCapacity = baseInactiveCapacity;

        foreach (GameObject go in activeInstances)
            if (go != null)
                Object.Destroy(go);
        activeInstances.Clear();

        while (inactiveInstances.Count > 0)
        {
            GameObject go = inactiveInstances.Dequeue();
            if (go != null)
                Object.Destroy(go);
        }

        if (prefabHandle != null)
        {
            prefabHandle.Release();
            prefabHandle = null;
        }
    }

    #endregion
}
