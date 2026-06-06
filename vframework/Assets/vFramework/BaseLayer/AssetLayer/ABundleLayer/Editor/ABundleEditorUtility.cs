using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    /// <summary>
    /// Editor 工具：平台映射、配置清理。
    /// </summary>
    public static class ABundleEditorUtility
    {
        #region 平台映射

        public static BuildTarget ToBuildTarget(ABundlePlatform platform)
        {
            switch (platform)
            {
                case ABundlePlatform.iOS: return BuildTarget.iOS;
                case ABundlePlatform.Android: return BuildTarget.Android;
                default: return BuildTarget.StandaloneWindows64;
            }
        }

        public static BuildTarget ToBuildTarget(string platformName) =>
            ToBuildTarget(ABundlePlatformNames.Parse(platformName));

        public static string FromActiveBuildTarget() =>
            FromBuildTarget(EditorUserBuildSettings.activeBuildTarget);

        public static string FromBuildTarget(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.iOS: return ABundlePlatformNames.ToName(ABundlePlatform.iOS);
                case BuildTarget.Android: return ABundlePlatformNames.ToName(ABundlePlatform.Android);
                default: return ABundlePlatformNames.ToName(ABundlePlatform.Windows);
            }
        }

        #endregion

        #region 配置清理

        public class ClearResult
        {
            public bool Success;
            public string Message;
            public List<string> DeletedPaths = new();
        }

        public static ClearResult DeleteRulesXml(string rulesXmlAssetPath)
        {
            var result = new ClearResult();
            if (string.IsNullOrWhiteSpace(rulesXmlAssetPath))
            {
                result.Message = "规则 XML 路径为空";
                return result;
            }

            rulesXmlAssetPath = ABundleRulesXmlIO.NormalizeAssetPath(rulesXmlAssetPath);
            var fullPath = ABundleRulesXmlIO.ToFullPath(rulesXmlAssetPath);

            if (!File.Exists(fullPath))
            {
                result.Success = true;
                result.Message = $"规则 XML 不存在:\n{rulesXmlAssetPath}";
                return result;
            }

            if (rulesXmlAssetPath.StartsWith("Assets/"))
            {
                if (!AssetDatabase.DeleteAsset(rulesXmlAssetPath))
                {
                    result.Message = $"删除失败: {rulesXmlAssetPath}";
                    return result;
                }
            }
            else
            {
                File.Delete(fullPath);
                if (File.Exists(fullPath + ".meta"))
                {
                    File.Delete(fullPath + ".meta");
                }
            }

            result.DeletedPaths.Add(rulesXmlAssetPath);
            AssetDatabase.Refresh();
            result.Success = true;
            result.Message = $"已删除规则 XML:\n{rulesXmlAssetPath}";
            return result;
        }

        public static ClearResult DeleteDefaultRulesXml() =>
            DeleteRulesXml(ABundleRulesXmlIO.DefaultRulesRelativePath);

        public static ClearResult ClearBuildOutput(string outputAssetPath)
        {
            var result = new ClearResult();
            if (string.IsNullOrWhiteSpace(outputAssetPath))
            {
                result.Message = "输出目录路径为空";
                return result;
            }

            outputAssetPath = ABundleRulesXmlIO.NormalizeAssetPath(outputAssetPath);
            var fullPath = ABundleRulesXmlIO.ToFullPath(outputAssetPath);

            if (!Directory.Exists(fullPath))
            {
                result.Success = true;
                result.Message = $"输出目录不存在:\n{outputAssetPath}";
                return result;
            }

            ClearDirectoryContents(fullPath, result);
            AssetDatabase.Refresh();
            result.Success = true;
            result.Message = $"已清空:\n{outputAssetPath}";
            return result;
        }

        public static ClearResult ClearAllPlatformOutputs(string outputRootAssetPath)
        {
            var result = new ClearResult { Success = true };
            var messages = new List<string>();

            foreach (var platform in ABundlePlatformNames.All)
            {
                var platformPath = ABundlePathUtility.GetPlatformOutputAssetPath(outputRootAssetPath, platform);
                var sub = ClearBuildOutput(platformPath);
                messages.Add(sub.Message);
                result.DeletedPaths.AddRange(sub.DeletedPaths);
                if (!sub.Success)
                {
                    result.Success = false;
                }
            }

            result.Message = string.Join("\n\n", messages);
            return result;
        }

        public static ClearResult ClearAll(ABundleBuildRules rules, bool deleteXml, bool clearOutput)
        {
            var summary = new ClearResult { Success = true };
            var messages = new List<string>();

            if (deleteXml && rules != null && !string.IsNullOrEmpty(rules.RulesXmlPath))
            {
                var xmlResult = DeleteRulesXml(rules.RulesXmlPath);
                messages.Add(xmlResult.Message);
                summary.DeletedPaths.AddRange(xmlResult.DeletedPaths);
                summary.Success &= xmlResult.Success;
            }
            else if (deleteXml)
            {
                var xmlResult = DeleteDefaultRulesXml();
                messages.Add(xmlResult.Message);
                summary.DeletedPaths.AddRange(xmlResult.DeletedPaths);
            }

            if (clearOutput && rules != null)
            {
                var outResult = ClearAllPlatformOutputs(rules.OutputPath);
                messages.Add(outResult.Message);
                summary.DeletedPaths.AddRange(outResult.DeletedPaths);
                summary.Success &= outResult.Success;
            }

            summary.Message = string.Join("\n\n", messages);
            return summary;
        }

        static void ClearDirectoryContents(string fullPath, ClearResult result)
        {
            foreach (var file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                var relative = ToAssetRelativePath(file);
                if (!string.IsNullOrEmpty(relative) && relative.StartsWith("Assets/"))
                {
                    if (AssetDatabase.DeleteAsset(relative))
                    {
                        result.DeletedPaths.Add(relative);
                    }
                }
                else
                {
                    File.Delete(file);
                    result.DeletedPaths.Add(file);
                }
            }

            foreach (var meta in Directory.GetFiles(fullPath, "*.meta", SearchOption.AllDirectories))
            {
                var assetPath = meta.Substring(0, meta.Length - 5);
                if (!File.Exists(assetPath))
                {
                    File.Delete(meta);
                }
            }
        }

        static string ToAssetRelativePath(string fullPath)
        {
            fullPath = fullPath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            return fullPath.StartsWith(dataPath) ? "Assets" + fullPath.Substring(dataPath.Length) : null;
        }

        #endregion
    }

    /// <summary>兼容旧调用名。</summary>
    public static class ABundlePlatformUtility
    {
        public static BuildTarget ToBuildTarget(ABundlePlatform p) => ABundleEditorUtility.ToBuildTarget(p);
        public static BuildTarget ToBuildTarget(string n) => ABundleEditorUtility.ToBuildTarget(n);
        public static string FromActiveBuildTarget() => ABundleEditorUtility.FromActiveBuildTarget();
        public static string FromBuildTarget(BuildTarget t) => ABundleEditorUtility.FromBuildTarget(t);
    }
}
