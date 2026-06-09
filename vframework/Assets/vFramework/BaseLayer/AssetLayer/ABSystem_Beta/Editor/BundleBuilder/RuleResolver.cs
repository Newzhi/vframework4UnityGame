using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RuleResolver
{
    public const string BundleSuffix = ".bundle";

    public static List<AssetBundleBuild> Resolve(BuildSetting setting)
    {
        switch (setting.packingRule)
        {
            case PackingRule.Detailed:
                return ResolveDetailed(setting.targetDirectory);
            case PackingRule.Custom:
                return ResolveCustom(setting.customItems);
            default:
                return ResolveDefault(setting.targetDirectory);
        }
    }

    public static Dictionary<BuildMode, List<AssetBundleBuild>> ResolveCustomGrouped(List<BundleConfigItem> items)
    {
        Dictionary<BuildMode, List<AssetBundleBuild>> grouped = new Dictionary<BuildMode, List<AssetBundleBuild>>
        {
            { BuildMode.EditorTest, new List<AssetBundleBuild>() },
            { BuildMode.DeviceDebug, new List<AssetBundleBuild>() },
            { BuildMode.CdnHotUpdate, new List<AssetBundleBuild>() },
            { BuildMode.DlcPackage, new List<AssetBundleBuild>() },
        };

        if (items == null)
            return grouped;

        foreach (BundleConfigItem item in items)
        {
            List<AssetBundleBuild> targetList = grouped[item.buildMode];
            AddCustomItemBuilds(item, targetList);
        }

        return grouped;
    }

    public static List<AssetBundleBuild> ResolveDefault(string targetFolder)
    {
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        if (!AssetDatabase.IsValidFolder(targetFolder))
            return builds;

        string[] subFolders = AssetDatabase.GetSubFolders(targetFolder);
        foreach (string subFolder in subFolders)
            TryAddFolderBuild(subFolder, BundlePlatformPaths.NormalizeBundleName(Path.GetFileName(subFolder) + BundleSuffix), builds);

        return builds;
    }

    public static List<AssetBundleBuild> ResolveDetailed(string targetFolder)
    {
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        if (!AssetDatabase.IsValidFolder(targetFolder))
            return builds;

        List<string> folders = new List<string>();
        CollectAllSubFolders(targetFolder, folders);

        foreach (string folder in folders)
        {
            string relative = folder.Substring(targetFolder.Length).TrimStart('/');
            string bundleName = BundlePlatformPaths.NormalizeBundleName(
                string.IsNullOrEmpty(relative)
                    ? Path.GetFileName(folder) + BundleSuffix
                    : relative.Replace("/", "_") + BundleSuffix);

            TryAddFolderBuild(folder, bundleName, builds);
        }

        return builds;
    }

    public static List<AssetBundleBuild> ResolveCustom(List<BundleConfigItem> items)
    {
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        if (items == null)
            return builds;

        foreach (BundleConfigItem item in items)
            AddCustomItemBuilds(item, builds);

        return builds;
    }

    static void AddCustomItemBuilds(BundleConfigItem item, List<AssetBundleBuild> builds)
    {
        if (string.IsNullOrEmpty(item.assetPath) || string.IsNullOrEmpty(item.bundleName))
            return;

        string bundleName = BundlePlatformPaths.NormalizeBundleName(
            item.bundleName.EndsWith(BundleSuffix)
                ? item.bundleName
                : item.bundleName + BundleSuffix);

        if (AssetDatabase.IsValidFolder(item.assetPath))
        {
            TryAddFolderBuild(item.assetPath, bundleName, builds);
        }
        else if (AssetDatabase.LoadMainAssetAtPath(item.assetPath) != null)
        {
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = new[] { item.assetPath }
            });
        }
    }

    static void CollectAllSubFolders(string folder, List<string> result)
    {
        result.Add(folder);
        foreach (string sub in AssetDatabase.GetSubFolders(folder))
            CollectAllSubFolders(sub, result);
    }

    static void TryAddFolderBuild(string folder, string bundleName, List<AssetBundleBuild> builds)
    {
        string[] assetPaths = BundleBuilder.CollectAssetPaths(folder);
        if (assetPaths.Length == 0)
            return;

        builds.Add(new AssetBundleBuild
        {
            assetBundleName = BundlePlatformPaths.NormalizeBundleName(bundleName),
            assetNames = assetPaths
        });
    }
}
