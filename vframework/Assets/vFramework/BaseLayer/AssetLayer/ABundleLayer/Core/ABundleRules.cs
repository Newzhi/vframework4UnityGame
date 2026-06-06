using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    #region 规则数据

    [Serializable]
    [XmlRoot("ABundleBuildRules")]
    public class ABundleBuildRules
    {
        public int Version = 1;
        public string RulesXmlPath;
        public string RootFolder = "Assets/AssetBundle";
        public string OutputPath = "Assets/StreamingAssets/AssetBundles";
        public ABundleLoadMode LoadMode = ABundleLoadMode.RuntimeBundle;
        public string CatalogFileName = "AssetCatalog.json";
        public string PlatformManifestFileName = "AssetBundles";
        public string BuildTarget = "Windows";
        public ABundlePackMode PackMode = ABundlePackMode.ByTopLevelFolder;
        public string BundleNamePrefix = "demo";
        public bool GenerateCatalog = true;

        [XmlArray("CustomRules")]
        [XmlArrayItem("Rule")]
        public List<ABundleBuildRule> CustomRules = new();

        public string LocationMode = "RelativeToRoot";
    }

    [Serializable]
    public class ABundleBuildRule
    {
        public string FolderPath = "Assets/";
        public string BundleName = "bundle/name";
        public bool Recursive = true;
        public string Description;
    }

    #endregion

    #region 路径工具

    public static class ABundlePathUtility
    {
        public static string GetPlatformOutputAssetPath(ABundleBuildRules rules)
        {
            var root = ABundleRulesXmlIO.NormalizeAssetPath(rules.OutputPath).TrimEnd('/');
            var platform = ABundlePlatformNames.Parse(rules.BuildTarget);
            return $"{root}/{ABundlePlatformNames.ToName(platform)}";
        }

        public static string GetPlatformOutputAssetPath(string outputRoot, string platformName)
        {
            var root = ABundleRulesXmlIO.NormalizeAssetPath(outputRoot).TrimEnd('/');
            platformName = ABundlePlatformNames.Parse(platformName).ToString();
            return $"{root}/{platformName}";
        }

        public static string ToFullPath(string assetPath) => ABundleRulesXmlIO.ToFullPath(assetPath);

        public static string GetReportFileName() => "ABundleBuildReport.json";
    }

    #endregion

    #region XML 读写

    public static class ABundleRulesXmlIO
    {
        public const string DefaultRulesRelativePath =
            "Assets/vFramework/BaseLayer/AssetLayer/ABundleLayer/Editor/Config/ABundleBuildRules.xml";

        public static ABundleBuildRules CreateDefault() =>
            new()
            {
                RulesXmlPath = DefaultRulesRelativePath,
                RootFolder = "Assets/AssetBundle",
                OutputPath = "Assets/StreamingAssets/AssetBundles",
                BuildTarget = "Windows",
                LoadMode = ABundleLoadMode.RuntimeBundle,
                PackMode = ABundlePackMode.ByTopLevelFolder,
                BundleNamePrefix = "demo",
                GenerateCatalog = true,
            };

        public static void Save(string xmlPath, ABundleBuildRules rules)
        {
            rules.RulesXmlPath = NormalizeAssetPath(xmlPath);
            var fullPath = ToFullPath(xmlPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? fullPath);

            var serializer = new XmlSerializer(typeof(ABundleBuildRules));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false),
            };

            using var writer = XmlWriter.Create(fullPath, settings);
            serializer.Serialize(writer, rules);
            Debug.Log($"[ABundle] 规则 XML 已保存: {rules.RulesXmlPath}");
        }

        public static ABundleBuildRules Load(string xmlPath)
        {
            var fullPath = ToFullPath(xmlPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[ABundle] 找不到规则 XML: {xmlPath}");
                return null;
            }

            var serializer = new XmlSerializer(typeof(ABundleBuildRules));
            using var reader = XmlReader.Create(fullPath);
            var rules = (ABundleBuildRules)serializer.Deserialize(reader);
            rules.RulesXmlPath = NormalizeAssetPath(xmlPath);
            return rules;
        }

        public static string ToFullPath(string assetOrFullPath)
        {
            if (Path.IsPathRooted(assetOrFullPath))
            {
                return assetOrFullPath.Replace('\\', '/');
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, assetOrFullPath))
                .Replace('\\', '/');
        }

        public static string NormalizeAssetPath(string path) => path.Replace('\\', '/').Trim();
    }

    #endregion
}
