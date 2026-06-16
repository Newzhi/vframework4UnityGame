using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CatalogueWriter
{
    public const string CatalogueAssetPath = BundleBuilder.SystemRoot + "/BundleRuleConfig/Catalogue/AssetCatalog.json";
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";

    static AssetCatalog lastBuiltCatalog;

    // TODO: 清单输出后续改二进制格式，见 MainRoadmap.md P3-12 / CatalogueReference.md §六。

    /// <summary>供 Pipeline 写 Manifest 时读取刚构建的 bundles[] 完整性字段。</summary>
    public static AssetCatalog LoadLastBuiltCatalog()
    {
        return lastBuiltCatalog;
    }

    public static bool Write(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest = null)
    {
        return Write(setting, builds, bundleRoot, manifest, Guid.NewGuid().ToString("N"), null, setting.buildMode);
    }

    public static bool Write(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest,
        string buildId,
        Dictionary<string, int> bundlePriorities,
        BuildMode modeOverride)
    {
        if (!TryBuildCatalog(
                setting,
                builds,
                bundleRoot,
                manifest,
                buildId,
                bundlePriorities,
                modeOverride,
                out AssetCatalog catalog,
                out string errorMessage))
        {
            Debug.LogError("Catalogue write failed: " + errorMessage);
            return false;
        }

        lastBuiltCatalog = catalog;
        string json = JsonUtility.ToJson(catalog, true);

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string catalogueFullPath = Path.GetFullPath(Path.Combine(projectRoot, CatalogueAssetPath));
        string catalogueDir = Path.GetDirectoryName(catalogueFullPath);
        if (!Directory.Exists(catalogueDir))
            Directory.CreateDirectory(catalogueDir);

        File.WriteAllText(catalogueFullPath, json);

        string runtimeCatalogueDir = Path.Combine(bundleRoot, "Catalogue");
        if (!Directory.Exists(runtimeCatalogueDir))
            Directory.CreateDirectory(runtimeCatalogueDir);

        File.WriteAllText(Path.Combine(runtimeCatalogueDir, RuntimeCatalogueFileName), json);
        AssetDatabase.Refresh();
        return true;
    }

    public static bool TryBuildCatalog(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest,
        out AssetCatalog catalog,
        out string errorMessage)
    {
        return TryBuildCatalog(
            setting,
            builds,
            bundleRoot,
            manifest,
            Guid.NewGuid().ToString("N"),
            null,
            setting != null ? setting.buildMode : BuildMode.DeviceDebug,
            out catalog,
            out errorMessage);
    }

    public static bool TryBuildCatalog(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest,
        string buildId,
        Dictionary<string, int> bundlePriorities,
        BuildMode modeOverride,
        out AssetCatalog catalog,
        out string errorMessage)
    {
        catalog = null;
        errorMessage = null;

        if (setting == null)
        {
            errorMessage = "BuildSetting is null.";
            return false;
        }

        List<AssetCatalogEntry> entries = new List<AssetCatalogEntry>();

        if (builds != null)
        {
            foreach (AssetBundleBuild build in builds)
            {
                foreach (string assetPath in build.assetNames)
                {
                    entries.Add(new AssetCatalogEntry
                    {
                        assetPath = assetPath,
                        bundleName = BundlePlatformPaths.NormalizeBundleName(build.assetBundleName),
                        assetName = Path.GetFileNameWithoutExtension(assetPath)
                    });
                }
            }
        }

        CatalogueValidator.ValidationResult validation = CatalogueValidator.ValidateEntries(
            entries,
            setting.targetDirectory,
            setting.loadPathDuplicateAsError);

        foreach (CatalogueValidator.LoadPathDuplicate duplicate in validation.loadPathDuplicates)
        {
            string message = "Duplicate loadPath '" + duplicate.loadPath + "' for assets: "
                + duplicate.firstAssetPath + " and " + duplicate.secondAssetPath;

            if (setting.loadPathDuplicateAsError)
                Debug.LogError(message);
            else
                Debug.LogWarning(message);
        }

        if (validation.hasErrors)
        {
            errorMessage = "Catalogue validation failed due to duplicate loadPath entries.";
            return false;
        }

        if (!TryBuildBundleDependencies(
                setting,
                manifest,
                builds,
                setting.useTopologicalSort,
                bundlePriorities,
                out BundleCatalogInfo[] bundles,
                out errorMessage))
            return false;

        foreach (BundleCatalogInfo info in bundles)
            BuildManifestService.FillBundleIntegrity(bundleRoot, info);

        catalog = new AssetCatalog
        {
            version = setting.version,
            buildNumber = setting.buildNumber,
            platform = setting.platform.ToString(),
            buildMode = modeOverride.ToString(),
            packingRule = setting.packingRule.ToString(),
            bundleRoot = bundleRoot,
            resourceRoot = setting.targetDirectory,
            buildId = buildId,
            compressionMode = setting.compressionMode.ToString(),
            cdnBaseUrl = setting.cdnBaseUrl ?? string.Empty,
            entries = entries.ToArray(),
            bundles = bundles
        };

        string jsonWithoutHash = JsonUtility.ToJson(catalog, true);
        catalog.catalogueHash = BuildHashCalculator.ComputeTextSha256(jsonWithoutHash);

        return true;
    }

    static bool TryBuildBundleDependencies(
        BuildSetting setting,
        AssetBundleManifest manifest,
        AssetBundleBuild[] builds,
        bool useTopologicalSort,
        Dictionary<string, int> bundlePriorities,
        out BundleCatalogInfo[] bundles,
        out string errorMessage)
    {
        bundles = new BundleCatalogInfo[0];
        errorMessage = null;

        if (manifest == null || builds == null || builds.Length == 0)
            return true;

        Dictionary<string, List<string>> directGraph = BuildDirectDependencyGraph(manifest, builds);
        List<BundleCatalogInfo> bundleList = new List<BundleCatalogInfo>();

        bool directOnly = setting != null && setting.useDirectDependenciesOnly;

        foreach (AssetBundleBuild build in builds)
        {
            string bundleName = BundlePlatformPaths.NormalizeBundleName(build.assetBundleName);
            HashSet<string> allDepSet = CollectAllDependencies(manifest, build.assetBundleName, bundleName);
            HashSet<string> directDepSet = directOnly
                ? CollectDirectDependencies(manifest, build.assetBundleName, bundleName)
                : allDepSet;

            string[] depNames;
            string[] depAllNames = null;

            if (useTopologicalSort)
            {
                var closure = new HashSet<string>(directDepSet, StringComparer.OrdinalIgnoreCase);
                if (!BundleDependencyTopology.TryTopologicalSort(closure, directGraph, out depNames, out string cycleHint))
                {
                    errorMessage = "Dependency cycle detected for bundle " + bundleName
                        + (string.IsNullOrEmpty(cycleHint) ? "" : " near " + cycleHint);
                    return false;
                }

                if (!directOnly && !BundleDependencyTopology.SetsEqual(depNames, allDepSet))
                {
                    errorMessage = "Topological sort changed dependency set for bundle: " + bundleName;
                    return false;
                }

                if (directOnly)
                {
                    var allClosure = new HashSet<string>(allDepSet, StringComparer.OrdinalIgnoreCase);
                    if (!BundleDependencyTopology.TryTopologicalSort(allClosure, directGraph, out depAllNames, out cycleHint))
                        depAllNames = allDepSet.ToArray();
                }
            }
            else
            {
                depNames = directDepSet.ToArray();
                if (directOnly)
                    depAllNames = allDepSet.ToArray();
            }

            int priority = (int)ResourcePriority.Normal;
            if (bundlePriorities != null && bundlePriorities.TryGetValue(bundleName, out int resolved))
                priority = resolved;

            var info = new BundleCatalogInfo
            {
                bundleName = bundleName,
                dependencies = depNames,
                resourcePriority = priority
            };

            if (directOnly && depAllNames != null)
                info.dependenciesAll = depAllNames;

            bundleList.Add(info);
        }

        bundles = bundleList.ToArray();
        return true;
    }

    static Dictionary<string, List<string>> BuildDirectDependencyGraph(AssetBundleManifest manifest, AssetBundleBuild[] builds)
    {
        List<(string bundleName, string[] directDependencies)> rows =
            new List<(string, string[])>();

        foreach (AssetBundleBuild build in builds)
        {
            string[] direct = manifest.GetDirectDependencies(build.assetBundleName);
            rows.Add((build.assetBundleName, direct));
        }

        return BundleDependencyTopology.CreateDirectDependencyGraph(rows);
    }

    static HashSet<string> CollectAllDependencies(AssetBundleManifest manifest, string rawBundleName, string normalizedBundleName)
    {
        return CollectDependencies(manifest, rawBundleName, normalizedBundleName, allDependencies: true);
    }

    static HashSet<string> CollectDirectDependencies(AssetBundleManifest manifest, string rawBundleName, string normalizedBundleName)
    {
        return CollectDependencies(manifest, rawBundleName, normalizedBundleName, allDependencies: false);
    }

    static HashSet<string> CollectDependencies(
        AssetBundleManifest manifest,
        string rawBundleName,
        string normalizedBundleName,
        bool allDependencies)
    {
        HashSet<string> depSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] deps = allDependencies
            ? manifest.GetAllDependencies(rawBundleName)
            : manifest.GetDirectDependencies(rawBundleName);

        foreach (string dep in deps)
        {
            if (dep == rawBundleName)
                continue;

            string depFileName = BundlePlatformPaths.NormalizeBundleName(Path.GetFileName(dep));
            if (string.IsNullOrEmpty(depFileName))
                continue;

            if (string.Equals(depFileName, normalizedBundleName, StringComparison.OrdinalIgnoreCase))
                continue;

            depSet.Add(depFileName);
        }

        return depSet;
    }
}
