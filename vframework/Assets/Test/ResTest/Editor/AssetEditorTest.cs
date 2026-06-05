using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor 菜单：把打了 AB 标签的资源打成包，输出到 StreamingAssets。
/// 使用：菜单 vFramework → Build Test AB，再 Play ResTest 场景点按钮加载。
/// </summary>
public static class AssetEditorTest
{
    const string OutputPath = "Assets/StreamingAssets/AssetBundles";

    [MenuItem("vFramework/Build Test AB")]
    public static void Build()
    {
        Directory.CreateDirectory(OutputPath);

        BuildPipeline.BuildAssetBundles(
            OutputPath,
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.Refresh();
        Debug.Log($"[AB] 打包完成: {Path.GetFullPath(OutputPath)}");
    }
}
