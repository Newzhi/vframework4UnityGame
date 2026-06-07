// ABundlePacker.cs — ② 打包器（Editor / Layer2_Packer）
// 用途：按规则执行校验、打标签、BuildAssetBundles、生成 Catalog 与打包报告。
// #region：资源过滤 → 打标签 → Catalog → 报告 → 打包入口

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    #region 资源过滤 - BundleAssetFilter

    /// <summary>
    /// 判断某资源是否允许打入 AssetBundle。
    ///
    /// 为什么需要过滤？
    /// ─────────────────────────────────────────────────────────────
    /// 1. Unity 禁止将「脚本源文件」打进 AB
    ///    .cs / MonoScript 属于工程代码，编译进程序集，不能作为 AB 内容导出。
    ///    若对 TestUI.cs 打 assetBundleName，会报：
    ///    "Script assets cannot be included in AssetBundles"。
    ///
    /// 2. 脚本应留在主工程，Prefab 只保留对脚本的「引用」
    ///    运行时加载 Prefab 时，Unity 用已编译的类型挂载组件，不需要把 .cs 打进包。
    ///
    /// 3. 工程配置类文件不是游戏资源
    ///    .asmdef、.dll、.meta 等不应参与分包，否则 Build 报错或产生无意义空包。
    ///
    /// 4. FindAssets("") 会扫到目录下「一切 GUID 资源」
    ///    包括脚本、默认资源等，必须显式排除。
    /// ─────────────────────────────────────────────────────────────
    /// </summary>
    public static class BundleAssetFilter
    {
        /// <summary>不参与 AB 的扩展名（小写，含点）。</summary>
        static readonly string[] ExcludedExtensions =
        {
            ".cs",
            ".js",
            ".boo",
            ".dll",
            ".asmdef",
            ".asmref",
            ".rsp",
            ".meta",
            ".md",
            ".gitkeep",
            ".gitignore",
        };

        /// <summary>该路径是否应参与 AB 打标签 / 写入 Catalog。</summary>
        public static bool CanIncludeInAssetBundle(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            assetPath = assetPath.Replace('\\', '/');

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return false;
            }

            if (IsExcludedExtension(assetPath))
            {
                return false;
            }

            if (IsUnderEditorFolder(assetPath))
            {
                return false;
            }

            var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (mainType == null)
            {
                return false;
            }

            // 脚本资源
            if (mainType == typeof(MonoScript))
            {
                return false;
            }

            // 空占位、部分内部资源
            if (mainType == typeof(DefaultAsset))
            {
                return false;
            }

            // 程序集定义
            if (mainType.Name == "AssemblyDefinitionAsset" || mainType.Name == "AssemblyDefinitionReferenceAsset")
            {
                return false;
            }

            return true;
        }

        public static string GetSkipReason(string assetPath)
        {
            if (IsExcludedExtension(assetPath))
            {
                return $"扩展名不参与 AB: {Path.GetExtension(assetPath)}";
            }

            if (IsUnderEditorFolder(assetPath))
            {
                return "位于 Editor 目录";
            }

            var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (mainType == typeof(MonoScript))
            {
                return "脚本 MonoScript";
            }

            if (mainType == typeof(DefaultAsset))
            {
                return "DefaultAsset";
            }

            return mainType?.Name ?? "未知类型";
        }

        static bool IsExcludedExtension(string assetPath)
        {
            var ext = Path.GetExtension(assetPath);
            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            ext = ext.ToLowerInvariant();
            for (var i = 0; i < ExcludedExtensions.Length; i++)
            {
                if (ext == ExcludedExtensions[i])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>跳过 Assets/.../Editor/ 下的资源（通常为工具脚本，不打 AB）。</summary>
        static bool IsUnderEditorFolder(string assetPath)
        {
            const string editorSegment = "/Editor/";
            return assetPath.IndexOf(editorSegment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    #endregion

    #region 打标签 - BundleLabelApplier

    /// <summary>
    /// 按 ABundleBuildRules 将 assetBundleName 写入 AssetImporter。
    /// </summary>
    public static class BundleLabelApplier
    {
        /// <summary>根据 PackMode 解析出「资源路径 → 包名」映射并打标签。</summary>
        public static void Apply(ABundleBuildRules rules)
        {
            if (rules == null)
            {
                Debug.LogError("[ABundle] 规则为空");
                return;
            }

            var mappings = BuildMappings(rules);
            var count = 0;
            foreach (var kv in mappings)
            {
                if (ApplyBundleName(kv.Key, kv.Value))
                {
                    count++;
                }
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ABundle] 已打标签 {count} 个资源（映射 {mappings.Count} 条，扫描时已过滤不可打包类型）");
        }

        static Dictionary<string, string> BuildMappings(ABundleBuildRules rules)
        {
            var result = new Dictionary<string, string>();
            var root = ABundleRulesXmlIO.NormalizeAssetPath(rules.RootFolder);

            switch (rules.PackMode)
            {
                case ABundlePackMode.ByTopLevelFolder:
                    BuildTopLevelFolderMappings(root, rules.BundleNamePrefix, result);
                    break;
                case ABundlePackMode.ByDirectoryTree:
                    BuildDirectoryTreeMappings(root, rules.BundleNamePrefix, result);
                    break;
                case ABundlePackMode.SingleRootBundle:
                    BuildFolderMappings(root, true, ComposeBundleName(rules.BundleNamePrefix, Path.GetFileName(root)), result);
                    break;
                case ABundlePackMode.CustomRules:
                    BuildCustomRuleMappings(rules.CustomRules, result);
                    break;
            }

            return result;
        }

        static void BuildTopLevelFolderMappings(string root, string prefix, Dictionary<string, string> result)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                Debug.LogError($"[ABundle] 根目录不存在: {root}");
                return;
            }

            // 根目录下直接放的资源 → prefix/root文件夹名
            var rootBundle = ComposeBundleName(prefix, Path.GetFileName(root));
            BuildFolderMappings(root, false, rootBundle, result);

            // 每个一级子文件夹 → prefix/子文件夹名
            foreach (var subFolder in GetSubFolders(root))
            {
                var folderName = Path.GetFileName(subFolder);
                var bundleName = ComposeBundleName(prefix, folderName);
                BuildFolderMappings(subFolder, true, bundleName, result);
            }
        }

        static void BuildDirectoryTreeMappings(string root, string prefix, Dictionary<string, string> result)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                Debug.LogError($"[ABundle] 根目录不存在: {root}");
                return;
            }

            CollectFolderMappings(root, root, prefix, result);
        }

        static void CollectFolderMappings(string root, string currentFolder, string prefix, Dictionary<string, string> result)
        {
            var relative = currentFolder.Substring(root.Length).TrimStart('/');
            var bundleSuffix = string.IsNullOrEmpty(relative)
                ? Path.GetFileName(root)
                : relative.Replace('\\', '/');
            var bundleName = ComposeBundleName(prefix, bundleSuffix);
            BuildFolderMappings(currentFolder, true, bundleName, result);

            foreach (var sub in GetSubFolders(currentFolder))
            {
                CollectFolderMappings(root, sub, prefix, result);
            }
        }

        static void BuildCustomRuleMappings(List<ABundleBuildRule> rules, Dictionary<string, string> result)
        {
            if (rules == null)
            {
                return;
            }

            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (string.IsNullOrEmpty(rule.FolderPath) || string.IsNullOrEmpty(rule.BundleName))
                {
                    continue;
                }

                BuildFolderMappings(rule.FolderPath, rule.Recursive, rule.BundleName, result);
            }
        }

        static void BuildFolderMappings(string folder, bool recursive, string bundleName, Dictionary<string, string> result)
        {
            folder = ABundleRulesXmlIO.NormalizeAssetPath(folder);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[ABundle] 跳过无效文件夹: {folder}");
                return;
            }

            var search = recursive ? folder : folder;
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { search });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                if (!recursive && Path.GetDirectoryName(path)?.Replace('\\', '/') != folder)
                {
                    continue;
                }

                // 脚本、asmdef、Editor 目录等不能打进 AB，见 BundleAssetFilter 说明
                if (!BundleAssetFilter.CanIncludeInAssetBundle(path))
                {
                    continue;
                }

                result[path] = bundleName;
            }
        }

        static bool ApplyBundleName(string assetPath, string bundleName)
        {
            if (!BundleAssetFilter.CanIncludeInAssetBundle(assetPath))
            {
                return false;
            }

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                return false;
            }

            if (importer.assetBundleName == bundleName)
            {
                return false;
            }

            importer.assetBundleName = bundleName;
            importer.SaveAndReimport();
            return true;
        }

        static List<string> GetSubFolders(string folder)
        {
            var list = new List<string>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return list;
            }

            var fullPath = ABundleRulesXmlIO.ToFullPath(folder);
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                var name = Path.GetFileName(dir);
                if (name == ".git" || name.EndsWith(".meta"))
                {
                    continue;
                }

                list.Add($"{folder}/{name}".Replace('\\', '/'));
            }

            return list;
        }

        static string ComposeBundleName(string prefix, string suffix)
        {
            prefix = (prefix ?? string.Empty).Trim().Trim('/');
            suffix = (suffix ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
            return string.IsNullOrEmpty(prefix) ? suffix : $"{prefix}/{suffix}";
        }
    }

    #endregion

    #region Catalog 生成 - CatalogGenerator

    /// <summary>
    /// Build 完成后生成 AssetCatalog.json（location 索引 + 包清单）。
    /// </summary>
    public static class CatalogGenerator
    {
        public static AssetCatalog Generate(ABundleBuildRules rules, string outputFullPath)
        {
            var catalog = new AssetCatalog
            {
                Version = 1,
                BuildTime = DateTime.UtcNow.ToString("o"),
                Platform = rules.BuildTarget,
            };

            FillBundleInfos(catalog, outputFullPath, rules.PlatformManifestFileName);
            FillLocations(catalog, rules);
            catalog.BuildRuntimeIndex();
            return catalog;
        }

        public static void SaveToJson(AssetCatalog catalog, string jsonFullPath)
        {
            var json = JsonUtility.ToJson(catalog, true);
            Directory.CreateDirectory(Path.GetDirectoryName(jsonFullPath) ?? outputFallback(jsonFullPath));
            File.WriteAllText(jsonFullPath, json);
            Debug.Log($"[ABundle] Catalog 已写入: {jsonFullPath}");
        }

        static string outputFallback(string path) => path;

        static void FillBundleInfos(AssetCatalog catalog, string outputFullPath, string manifestFileName)
        {
            var manifestPath = Path.Combine(outputFullPath, manifestFileName);
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[ABundle] 未找到 Manifest，跳过 BundleInfos");
                return;
            }

            var manifestBundle = AssetBundle.LoadFromFile(manifestPath);
            if (manifestBundle == null)
            {
                return;
            }

            var manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);
            if (manifest == null)
            {
                return;
            }

            var allBundles = manifest.GetAllAssetBundles();
            for (var i = 0; i < allBundles.Length; i++)
            {
                var bundleName = allBundles[i];
                var hash = manifest.GetAssetBundleHash(bundleName);
                var deps = manifest.GetAllDependencies(bundleName);
                var filePath = Path.Combine(outputFullPath, bundleName);
                long size = 0;
                if (File.Exists(filePath))
                {
                    size = new FileInfo(filePath).Length;
                }

                catalog.Bundles.Add(new BundleInfo
                {
                    BundleName = bundleName,
                    Hash = hash.ToString(),
                    Size = size,
                    Dependencies = deps,
                    FileName = bundleName,
                });
            }
        }

        static void FillLocations(AssetCatalog catalog, ABundleBuildRules rules)
        {
            var root = ABundleRulesXmlIO.NormalizeAssetPath(rules.RootFolder);
            if (!AssetDatabase.IsValidFolder(root))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets(string.Empty, new[] { root });
            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                if (!BundleAssetFilter.CanIncludeInAssetBundle(assetPath))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null || string.IsNullOrEmpty(importer.assetBundleName))
                {
                    continue;
                }

                var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                catalog.Locations.Add(new AssetLocationEntry
                {
                    Location = BuildLocation(assetPath, rules),
                    BundleName = importer.assetBundleName,
                    AssetName = Path.GetFileNameWithoutExtension(assetPath),
                    AssetType = mainType?.Name ?? "Object",
                    SourceAssetPath = assetPath,
                });
            }
        }

        static string BuildLocation(string assetPath, ABundleBuildRules rules)
        {
            assetPath = assetPath.Replace('\\', '/');
            if (rules.LocationMode == "AssetPathWithoutExtension")
            {
                return assetPath.Replace("Assets/", string.Empty);
            }

            // RelativeToRoot：相对 RootFolder 的路径（无扩展名、小写）
            var root = ABundleRulesXmlIO.NormalizeAssetPath(rules.RootFolder);
            if (assetPath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                var relative = assetPath.Substring(root.Length + 1);
                return Path.ChangeExtension(relative, null).Replace('\\', '/').ToLowerInvariant();
            }

            return Path.ChangeExtension(assetPath.Replace("Assets/", string.Empty), null)
                .Replace('\\', '/')
                .ToLowerInvariant();
        }
    }

    #endregion

    #region 打包报告 - ABundleBuildReporter

    /// <summary>
    /// 打包前校验与打包后报告生成。
    /// </summary>
    public static class ABundleBuildReporter
    {
        public static ABundleBuildReport CreateEmpty(ABundleBuildRules rules) =>
            new()
            {
                Platform = rules?.BuildTarget ?? string.Empty,
                LoadMode = rules?.LoadMode.ToString() ?? string.Empty,
            };

        public static void ValidateBeforeBuild(ABundleBuildRules rules, ABundleBuildReport report)
        {
            var root = ABundleRulesXmlIO.NormalizeAssetPath(rules.RootFolder);
            if (!AssetDatabase.IsValidFolder(root))
            {
                report.Errors.Add($"资源根目录不存在: {root}");
                return;
            }

            if (string.IsNullOrWhiteSpace(rules.BundleNamePrefix))
            {
                report.Warnings.Add("包名前缀为空，将直接使用文件夹名作为包名");
            }

            if (rules.PackMode == ABundlePackMode.CustomRules &&
                (rules.CustomRules == null || rules.CustomRules.Count == 0))
            {
                report.Errors.Add("CustomRules 模式下 CustomRules 列表不能为空");
            }

            var guids = AssetDatabase.FindAssets(string.Empty, new[] { root });
            var labeledCount = 0;
            var skippedCount = 0;

            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                if (!BundleAssetFilter.CanIncludeInAssetBundle(assetPath))
                {
                    skippedCount++;
                    continue;
                }

                labeledCount++;
            }

            if (labeledCount == 0)
            {
                report.Warnings.Add($"根目录下无有效可打包资源（已过滤 {skippedCount} 项）");
            }
        }

        public static void FillFromManifest(
            ABundleBuildReport report,
            AssetBundleManifest manifest,
            string outputFullPath,
            AssetCatalog catalog)
        {
            var allBundles = manifest.GetAllAssetBundles();
            report.BundleCount = allBundles.Length;
            report.TotalSizeBytes = 0;

            var locationByBundle = new Dictionary<string, int>();
            if (catalog?.Locations != null)
            {
                foreach (var loc in catalog.Locations)
                {
                    if (string.IsNullOrEmpty(loc.BundleName))
                    {
                        continue;
                    }

                    locationByBundle.TryGetValue(loc.BundleName, out var count);
                    locationByBundle[loc.BundleName] = count + 1;
                }
            }

            foreach (var bundleName in allBundles)
            {
                var hash = manifest.GetAssetBundleHash(bundleName);
                var deps = manifest.GetAllDependencies(bundleName);
                var filePath = Path.Combine(outputFullPath, bundleName);
                long size = 0;
                if (File.Exists(filePath))
                {
                    size = new FileInfo(filePath).Length;
                }

                report.TotalSizeBytes += size;
                report.Bundles.Add(new ABundleReportEntry
                {
                    BundleName = bundleName,
                    SizeBytes = size,
                    Hash = hash.ToString(),
                    Dependencies = deps,
                });

                if (size == 0)
                {
                    report.Warnings.Add($"空包或缺失文件: {bundleName}");
                }

                if (!locationByBundle.ContainsKey(bundleName))
                {
                    report.Warnings.Add($"包无 Catalog 条目: {bundleName}");
                }
            }

            // 重复 location 检测
            if (catalog?.Locations != null)
            {
                var dupes = catalog.Locations
                    .GroupBy(l => l.Location, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var dupe in dupes)
                {
                    report.Warnings.Add($"重复 location: {dupe}");
                }
            }
        }

        public static void SaveReport(ABundleBuildReport report, string fullPath)
        {
            var json = JsonUtility.ToJson(report, true);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? fullPath);
            File.WriteAllText(fullPath, json);
            Debug.Log($"[ABundle] 打包报告已写入: {fullPath}");
        }

        public static ABundleBuildReport LoadReport(string reportAssetPath)
        {
            var fullPath = ABundleRulesXmlIO.ToFullPath(reportAssetPath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var json = File.ReadAllText(fullPath);
            return JsonUtility.FromJson<ABundleBuildReport>(json);
        }

        public static string FormatSummary(ABundleBuildReport report)
        {
            if (report == null)
            {
                return "无报告";
            }

            var sizeMb = report.TotalSizeBytes / (1024f * 1024f);
            var lines = new List<string>
            {
                report.Success ? "状态: 成功" : "状态: 失败",
                $"平台: {report.Platform}",
                $"输出: {report.OutputPath}",
                $"包数量: {report.BundleCount}  总大小: {sizeMb:F2} MB",
                $"Location: {report.LocationCount}  耗时: {report.DurationSeconds:F1}s",
            };

            if (report.Warnings.Count > 0)
            {
                lines.Add($"警告 ({report.Warnings.Count}):");
                lines.AddRange(report.Warnings.Take(8).Select(w => "  · " + w));
                if (report.Warnings.Count > 8)
                {
                    lines.Add($"  … 另有 {report.Warnings.Count - 8} 条");
                }
            }

            if (report.Errors.Count > 0)
            {
                lines.Add($"错误 ({report.Errors.Count}):");
                lines.AddRange(report.Errors.Select(e => "  · " + e));
            }

            return string.Join("\n", lines);
        }
    }

    #endregion

    #region ② 打包器 - ABundlePacker

    /// <summary>
    /// ② 打包器：校验 → 打标签 → Build → Catalog → 报告。
    /// </summary>
    public static class ABundlePacker
    {
        public static ABundleBuildReport BuildFromRules(ABundleBuildRules rules)
        {
            var report = ABundleBuildReporter.CreateEmpty(rules);
            if (rules == null)
            {
                report.Errors.Add("规则为空");
                return report;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var buildTarget = ABundlePlatformUtility.ToBuildTarget(rules.BuildTarget);
                var outputAssetPath = ABundlePathUtility.GetPlatformOutputAssetPath(rules);
                var outputFullPath = ABundlePathUtility.ToFullPath(outputAssetPath);
                Directory.CreateDirectory(outputFullPath);
                report.OutputPath = outputAssetPath;

                ABundleBuildReporter.ValidateBeforeBuild(rules, report);
                if (report.Errors.Count > 0)
                {
                    return report;
                }

                Debug.Log($"[ABundle] 开始打包，模式={rules.PackMode}，平台={rules.BuildTarget}，输出={outputAssetPath}");
                BundleLabelApplier.Apply(rules);

                var manifest = BuildPipeline.BuildAssetBundles(
                    outputAssetPath,
                    BuildAssetBundleOptions.None,
                    buildTarget);

                if (manifest == null)
                {
                    report.Errors.Add("BuildPipeline 返回 null，打包失败");
                    Debug.LogError("[ABundle] BuildPipeline 返回 null");
                    return report;
                }

                AssetDatabase.Refresh();

                AssetCatalog catalog = null;
                if (rules.GenerateCatalog)
                {
                    catalog = CatalogGenerator.Generate(rules, outputFullPath);
                    var catalogPath = Path.Combine(outputFullPath, rules.CatalogFileName);
                    CatalogGenerator.SaveToJson(catalog, catalogPath);
                    report.CatalogPath = ABundleRulesXmlIO.NormalizeAssetPath(
                        $"{outputAssetPath}/{rules.CatalogFileName}");
                    report.LocationCount = catalog.Locations.Count;
                }

                ABundleBuildReporter.FillFromManifest(report, manifest, outputFullPath, catalog);
                report.Success = true;
                Debug.Log($"[ABundle] 打包完成，共 {report.BundleCount} 个包 → {outputAssetPath}");
            }
            catch (Exception ex)
            {
                report.Errors.Add(ex.Message);
                Debug.LogException(ex);
            }
            finally
            {
                sw.Stop();
                report.DurationSeconds = sw.Elapsed.TotalSeconds;
                report.BuildTime = DateTime.UtcNow.ToString("o");

                if (!string.IsNullOrEmpty(report.OutputPath))
                {
                    var fullPath = ABundlePathUtility.ToFullPath(report.OutputPath);
                    var reportPath = Path.Combine(fullPath, ABundlePathUtility.GetReportFileName());
                    ABundleBuildReporter.SaveReport(report, reportPath);
                    report.ReportPath = ABundleRulesXmlIO.NormalizeAssetPath(
                        $"{report.OutputPath}/{ABundlePathUtility.GetReportFileName()}");
                    AssetDatabase.Refresh();
                }
            }

            return report;
        }

        public static ABundleBuildReport BuildFromXml(string xmlPath)
        {
            var rules = ABundleRulesXmlIO.Load(xmlPath);
            if (rules == null)
            {
                var report = new ABundleBuildReport();
                report.Errors.Add($"无法加载 XML: {xmlPath}");
                return report;
            }

            return BuildFromRules(rules);
        }
    }

    /// <summary>兼容旧名称，请使用 <see cref="ABundlePacker"/>。</summary>
    public static class ABundleBuildPipeline
    {
        public static ABundleBuildReport BuildFromRules(ABundleBuildRules rules) =>
            ABundlePacker.BuildFromRules(rules);

        public static ABundleBuildReport BuildFromXml(string xmlPath) =>
            ABundlePacker.BuildFromXml(xmlPath);
    }

    #endregion
}
