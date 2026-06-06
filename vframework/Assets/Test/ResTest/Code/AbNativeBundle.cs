using System.IO;
using UnityEngine;

/// <summary>
/// Unity 原生 AssetBundle 加载封装（练习用，非 vFramework ResMgr）。
/// </summary>
public class AbNativeBundle
{
    public AssetBundle Bundle { get; private set; }
    public string BundleName { get; private set; }

    public static string GetBundleFilePath(string bundleName)
    {
        return Path.Combine(
            Application.streamingAssetsPath,
            AbTestConfig.StreamingBundleFolder,
            bundleName);
    }

    public bool Load(string bundleName)
    {
        if (Bundle != null)
        {
            return true;
        }

        BundleName = bundleName;
        var path = GetBundleFilePath(bundleName);
        Bundle = AssetBundle.LoadFromFile(path);
        if (Bundle == null)
        {
            Debug.LogError(
                $"[AB] LoadFromFile 失败: {path}\n" +
                "请确认：1) 资源已设 AssetBundle 名  2) 已执行 vFramework → Build Test AB");
            return false;
        }

        Debug.Log($"[AB] 已加载 Bundle: {bundleName}");
        return true;
    }

    public T LoadAsset<T>(string assetName) where T : Object
    {
        if (Bundle == null)
        {
            Debug.LogError("[AB] Bundle 未加载，请先 Load");
            return null;
        }

        var asset = Bundle.LoadAsset<T>(assetName);
        if (asset == null)
        {
            Debug.LogError(
                $"[AB] 包 {BundleName} 内找不到 {assetName}，请打开 {BundleName}.manifest 查看 Name");
        }

        return asset;
    }

    public void Unload(bool unloadAllLoadedObjects = false)
    {
        if (Bundle == null)
        {
            return;
        }

        Bundle.Unload(unloadAllLoadedObjects);
        Bundle = null;
        Debug.Log($"[AB] 已卸载 Bundle: {BundleName} (unloadAll={unloadAllLoadedObjects})");
        BundleName = null;
    }
}
