using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CatalogueWriter
{
    public const string CatalogueAssetPath = BundleBuilder.SystemRoot + "/BundleRuleConfig/Catalogue/AssetCatalog.json";
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";

    // TODO: 清单输出后续改二进制格式，见 Docs/TODO.md（若未建则见 Catalogue清单说明.md 第六节）。

    public static void Write(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest = null)
    {
        AssetCatalog catalog = BuildCatalog(setting, builds, bundleRoot, manifest);
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
    }

    static AssetCatalog BuildCatalog(
        BuildSetting setting,
        AssetBundleBuild[] builds,
        string bundleRoot,
        AssetBundleManifest manifest)
    {
        List<AssetCatalogEntry> entries = new List<AssetCatalogEntry>();

        foreach (AssetBundleBuild build in builds)
        {
            foreach (string assetPath in build.assetNames)
            {
                entries.Add(new AssetCatalogEntry
                {
                    assetPath = assetPath,
                    bundleName = build.assetBundleName,
                    assetName = Path.GetFileNameWithoutExtension(assetPath)
                });
            }
        }

        return new AssetCatalog
        {
            version = setting.version,
            buildNumber = setting.buildNumber,
            platform = setting.platform.ToString(),
            buildMode = setting.buildMode.ToString(),
            packingRule = setting.packingRule.ToString(),
            bundleRoot = bundleRoot,
            resourceRoot = setting.targetDirectory,
            entries = entries.ToArray(),
            bundles = BuildBundleDependencies(manifest, builds)
        };
    }

    static BundleCatalogInfo[] BuildBundleDependencies(AssetBundleManifest manifest, AssetBundleBuild[] builds)
    {
        if (manifest == null || builds == null || builds.Length == 0)
            return new BundleCatalogInfo[0];

        List<BundleCatalogInfo> bundles = new List<BundleCatalogInfo>();

        foreach (AssetBundleBuild build in builds)
        {
            string bundleName = build.assetBundleName;
            string[] deps = manifest.GetAllDependencies(bundleName);
            List<string> depNames = new List<string>();

            foreach (string dep in deps)
            {
                if (dep == bundleName)
                    continue;

                string depFileName = Path.GetFileName(dep);
                if (!string.IsNullOrEmpty(depFileName) && !depNames.Contains(depFileName))
                    depNames.Add(depFileName);
            }

            bundles.Add(new BundleCatalogInfo
            {
                bundleName = bundleName,
                dependencies = depNames.ToArray()
            });
        }

        return bundles.ToArray();
    }
}
