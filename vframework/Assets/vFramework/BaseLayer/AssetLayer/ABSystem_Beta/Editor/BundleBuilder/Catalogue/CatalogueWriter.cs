using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CatalogueWriter
{
    public const string CatalogueAssetPath = BundleBuilder.SystemRoot + "/BundleRuleConfig/Catalogue/AssetCatalog.json";
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";

    // TODO: 清单输出后续改二进制格式，见 Docs/TODO.md（若未建则见 Catalogue清单说明.md 第六节）。

    public static bool Write(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest = null)
    {
        if (!TryBuildCatalog(setting, builds, bundleRoot, manifest, out AssetCatalog catalog, out string errorMessage))
        {
            Debug.LogError("Catalogue write failed: " + errorMessage);
            return false;
        }

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

        if (!TryBuildBundleDependencies(manifest, builds, setting.useTopologicalSort, out BundleCatalogInfo[] bundles, out errorMessage))
            return false;

        catalog = new AssetCatalog
        {
            version = setting.version,
            buildNumber = setting.buildNumber,
            platform = setting.platform.ToString(),
            buildMode = setting.buildMode.ToString(),
            packingRule = setting.packingRule.ToString(),
            bundleRoot = bundleRoot,
            resourceRoot = setting.targetDirectory,
            entries = entries.ToArray(),
            bundles = bundles
        };

        return true;
    }

    static bool TryBuildBundleDependencies(
        AssetBundleManifest manifest,
        AssetBundleBuild[] builds,
        bool useTopologicalSort,
        out BundleCatalogInfo[] bundles,
        out string errorMessage)
    {
        bundles = new BundleCatalogInfo[0];
        errorMessage = null;

        if (manifest == null || builds == null || builds.Length == 0)
            return true;

        Dictionary<string, List<string>> directGraph = BuildDirectDependencyGraph(manifest, builds);
        List<BundleCatalogInfo> bundleList = new List<BundleCatalogInfo>();

        foreach (AssetBundleBuild build in builds)
        {
            string bundleName = BundlePlatformPaths.NormalizeBundleName(build.assetBundleName);
            HashSet<string> allDepSet = CollectAllDependencies(manifest, build.assetBundleName, bundleName);
            string[] depNames;

            if (useTopologicalSort)
            {
                var closure = new HashSet<string>(allDepSet, System.StringComparer.OrdinalIgnoreCase);
                if (!BundleDependencyTopology.TryTopologicalSort(closure, directGraph, out depNames, out string cycleHint))
                {
                    errorMessage = "Dependency cycle detected for bundle " + bundleName
                        + (string.IsNullOrEmpty(cycleHint) ? "" : " near " + cycleHint);
                    return false;
                }

                if (!BundleDependencyTopology.SetsEqual(depNames, allDepSet))
                {
                    errorMessage = "Topological sort changed dependency set for bundle: " + bundleName;
                    return false;
                }
            }
            else
            {
                depNames = new List<string>(allDepSet).ToArray();
            }

            bundleList.Add(new BundleCatalogInfo
            {
                bundleName = bundleName,
                dependencies = depNames
            });
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
        HashSet<string> allDepSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        string[] deps = manifest.GetAllDependencies(rawBundleName);

        foreach (string dep in deps)
        {
            if (dep == rawBundleName)
                continue;

            string depFileName = BundlePlatformPaths.NormalizeBundleName(Path.GetFileName(dep));
            if (string.IsNullOrEmpty(depFileName))
                continue;

            if (string.Equals(depFileName, normalizedBundleName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            allDepSet.Add(depFileName);
        }

        return allDepSet;
    }
}
