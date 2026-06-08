using System.IO;
using UnityEditor;
using UnityEngine;

public static class CatalogueWriter
{
    public const string CatalogueAssetPath = BundleBuilder.SystemRoot + "/BundleRuleConfig/Catalogue/AssetCatalog.json";
    public const string RuntimeCatalogueFileName = "AssetCatalog.json";

    // TODO: 清单输出后续改二进制格式，见 Docs/TODO.md（若未建则见 Catalogue清单说明.md 第六节）。

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
            // TODO: bundles = BuildBundleDependencies(manifest, builds)
        };
    }

    // TODO: 从 BuildPipeline 返回的 AssetBundleManifest 生成 BundleCatalogInfo[]。
    // 1) BundleBuilder.BuildByMode 保存 BuildAssetBundles 返回值；
    // 2) manifest.LoadAsset<AssetBundleManifest>("AssetBundleManifest")；
    // 3) 对每个 build.assetBundleName 调用 GetAllDependencies，Path.GetFileName 规范化；
    // 4) 写入 AssetCatalog.bundles。详见 Docs/Catalogue清单说明.md 第三节。
    //
    // static BundleCatalogInfo[] BuildBundleDependencies(
    //     AssetBundleManifest manifest, AssetBundleBuild[] builds) { ... }
}
