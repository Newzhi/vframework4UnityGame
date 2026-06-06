using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键写入 AB 演示标签，与 AbTestConfig 常量一致。
/// </summary>
public static class AbDemoLabelApplier
{
    [MenuItem("vFramework/AB Demo/Apply Demo AB Labels")]
    public static void ApplyLabels()
    {
        SetBundle("Assets/AssetBundle/UI/TestUI.prefab", AbTestConfig.UiTestUiRootBundle);
        SetBundle("Assets/AssetBundle/UI/Test/TestUI.prefab", AbTestConfig.UiTestUiAltBundle);
        SetBundle("Assets/AssetBundle/Icon", AbTestConfig.IconBundle);
        SetBundle("Assets/AssetBundle/Background", AbTestConfig.BackgroundBundle);
        SetBundle("Assets/AssetBundle/Atlas", AbTestConfig.AtlasBundleName);
        SetBundle("Assets/AssetBundle/Model/Ji.prefab", AbTestConfig.JiPrefabBundle);
        SetBundle("Assets/AssetBundle/Model/ji/cai/lambert2.mat", AbTestConfig.JiMatBundle);

        ClearBundle("Assets/AssetBundle/UI");

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        Debug.Log("[AB Demo] 标签已应用。请执行 vFramework → Build Test AB");
    }

    static void SetBundle(string assetPath, string bundleName)
    {
        var importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null)
        {
            Debug.LogWarning($"[AB Demo] 找不到: {assetPath}");
            return;
        }

        importer.assetBundleName = bundleName;
        importer.SaveAndReimport();
        Debug.Log($"[AB Demo] {assetPath} → {bundleName}");
    }

    static void ClearBundle(string folderPath)
    {
        var importer = AssetImporter.GetAtPath(folderPath);
        if (importer == null)
        {
            return;
        }

        importer.assetBundleName = string.Empty;
        importer.SaveAndReimport();
    }
}
