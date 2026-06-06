using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 演示 AssetBundleManifest：查询 AB 间依赖并按顺序 LoadFromFile。
/// </summary>
public static class AbManifestLoader
{
    private static AssetBundleManifest _manifest;
    private static readonly Dictionary<string, AssetBundle> Loaded = new();

    public static string GetBundleFilePath(string bundleName)
    {
        return Path.Combine(
            Application.streamingAssetsPath,
            AbTestConfig.StreamingBundleFolder,
            bundleName);
    }

    /// <summary>
    /// 加载平台总 Manifest（只需一次）。
    /// API：AssetBundle.LoadFromFile → LoadAsset&lt;AssetBundleManifest&gt;
    /// </summary>
    public static AssetBundleManifest GetManifest()
    {
        if (_manifest != null)
        {
            return _manifest;
        }

        var manifestBundlePath = GetBundleFilePath(AbTestConfig.PlatformManifestBundleFile);
        var manifestBundle = AssetBundle.LoadFromFile(manifestBundlePath);
        if (manifestBundle == null)
        {
            Debug.LogError($"[AB] 无法加载平台 Manifest 包: {manifestBundlePath}");
            return null;
        }

        // 固定资源名 "AssetBundleManifest"
        _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        manifestBundle.Unload(false);

        if (_manifest == null)
        {
            Debug.LogError("[AB] 包内找不到 AssetBundleManifest 资源");
        }

        return _manifest;
    }

    /// <summary>
    /// 递归加载 bundleName 及其全部依赖（问题3核心 API）。
    /// </summary>
    public static AssetBundle LoadBundleWithDependencies(string bundleName)
    {
        if (Loaded.TryGetValue(bundleName, out var cached) && cached != null)
        {
            return cached;
        }

        var manifest = GetManifest();
        if (manifest == null)
        {
            return LoadBundleDirect(bundleName);
        }

        // API：GetAllDependencies 返回该 AB 依赖的其它 AB 名（不含自身）
        var deps = manifest.GetAllDependencies(bundleName);
        for (var i = 0; i < deps.Length; i++)
        {
            LoadBundleWithDependencies(deps[i]);
        }

        return LoadBundleDirect(bundleName);
    }

    /// <summary>
    /// 仅 Load 主包、不递归依赖（Q3 对比实验）。仍写入 Loaded，UnloadAll 可正确卸载。
    /// </summary>
    public static AssetBundle LoadBundleMainOnly(string bundleName)
    {
        return LoadBundleDirect(bundleName);
    }

    private static AssetBundle LoadBundleDirect(string bundleName)
    {
        if (Loaded.TryGetValue(bundleName, out var cached) && cached != null)
        {
            return cached;
        }

        var path = GetBundleFilePath(bundleName);
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Debug.LogError($"[AB] LoadFromFile 失败: {path}");
            return null;
        }

        Loaded[bundleName] = bundle;
        Debug.Log($"[AB] LoadFromFile 成功: {bundleName}");
        return bundle;
    }

    public static void UnloadAll(bool unloadAllLoadedObjects = false)
    {
        foreach (var kv in Loaded)
        {
            kv.Value?.Unload(unloadAllLoadedObjects);
        }

        Loaded.Clear();
        _manifest = null;
        Debug.Log("[AB] 已卸载全部缓存 Bundle");
    }

    public static IReadOnlyCollection<string> GetLoadedBundleNames()
    {
        return Loaded.Keys;
    }
}
