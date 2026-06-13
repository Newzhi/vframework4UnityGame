using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池 Hierarchy 根节点约定；缓存查找结果避免重复 GameObject.Find。
/// </summary>
public static class PoolSceneRoots
{
    public const string RuntimeRootName = "PoolRuntime";

    static Transform runtimeRootCache;
    static readonly Dictionary<string, Transform> inactiveRootCache = new Dictionary<string, Transform>(8);
    static readonly Dictionary<string, Transform> activeRootCache = new Dictionary<string, Transform>(8);

    public static Transform GetOrCreateRuntimeRoot()
    {
        if (IsAlive(runtimeRootCache))
            return runtimeRootCache;

        GameObject existing = GameObject.Find(RuntimeRootName);
        if (existing != null)
        {
            runtimeRootCache = existing.transform;
            return runtimeRootCache;
        }

        runtimeRootCache = new GameObject(RuntimeRootName).transform;
        return runtimeRootCache;
    }

    public static Transform GetOrCreateInactiveRoot(string loadPath)
    {
        string childName = "Inactive_" + SanitizeLoadPath(loadPath);
        if (inactiveRootCache.TryGetValue(childName, out Transform cached) && IsAlive(cached))
            return cached;

        Transform runtime = GetOrCreateRuntimeRoot();
        Transform child = runtime.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(runtime, false);
            child = go.transform;
        }

        inactiveRootCache[childName] = child;
        return child;
    }

    public static Transform GetOrCreateActiveRoot(string logicalName)
    {
        string childName = "Active_" + logicalName;
        if (activeRootCache.TryGetValue(childName, out Transform cached) && IsAlive(cached))
            return cached;

        Transform runtime = GetOrCreateRuntimeRoot();
        Transform child = runtime.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(runtime, false);
            child = go.transform;
        }

        activeRootCache[childName] = child;
        return child;
    }

    /// <summary>场景切换或销毁 PoolRuntime 后清缓存。</summary>
    public static void ClearCache()
    {
        runtimeRootCache = null;
        inactiveRootCache.Clear();
        activeRootCache.Clear();
    }

    static bool IsAlive(Transform t) => t != null;

    static string SanitizeLoadPath(string loadPath)
    {
        if (string.IsNullOrEmpty(loadPath))
            return "Unknown";

        return loadPath.Replace('/', '_').Replace('\\', '_');
    }
}
