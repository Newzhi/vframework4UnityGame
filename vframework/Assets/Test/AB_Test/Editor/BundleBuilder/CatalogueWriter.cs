using System.IO;
using UnityEditor;
using UnityEngine;

public static class CatalogueWriter
{
    public const string CatalogueAssetPath = "Assets/Test/AB_Test/BundleRuleConfig/Catalogue/AssetCatalog.json";
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";

    public static void Write(BuildSetting setting, AssetBundleBuild[] builds, string bundleRoot)
    {
        AssetCatalog catalog = BuildCatalog(setting, builds, bundleRoot);
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

    static AssetCatalog BuildCatalog(BuildSetting setting, AssetBundleBuild[] builds, string bundleRoot)
    {
        System.Collections.Generic.List<AssetCatalogEntry> entries = new System.Collections.Generic.List<AssetCatalogEntry>();

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
            entries = entries.ToArray()
        };
    }
}
