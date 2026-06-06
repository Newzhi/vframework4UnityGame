using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    /// <summary>① 规则制定器。菜单：vFramework → AssetKit → ABundleBuilder</summary>
    public class ABundleRuleEditorWindow : EditorWindow
    {
        #region 字段

        ABundleBuildRules _rules;
        ABundleBuildReport _lastReport;
        Vector2 _scroll;
        Vector2 _rulesScroll;
        Vector2 _reportScroll;
        bool _showReport;

        static readonly string[] LocationModeLabels =
        {
            "RelativeToRoot（相对根目录）",
            "AssetPathWithoutExtension（Assets 相对路径）",
        };

        static readonly string[] LocationModeValues =
        {
            "RelativeToRoot",
            "AssetPathWithoutExtension",
        };

        static readonly string[] LoadModeLabels =
        {
            "EditorSimulation（Editor 直读工程资源）",
            "RuntimeBundle（真实 AB 加载）",
        };

        #endregion

        #region 菜单入口

        [MenuItem("vFramework/AssetKit/ABundleBuilder")]
        public static void Open()
        {
            var window = GetWindow<ABundleRuleEditorWindow>("ABundle Builder");
            window.minSize = new Vector2(520, 560);
            window.Show();
        }

        #endregion

        #region 生命周期

        void OnEnable()
        {
            if (_rules == null)
            {
                TryLoadDefaultRules();
            }
        }

        #endregion

        #region 界面绘制

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("ABundle Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "配置分包规则 → 按平台输出到子目录 → 生成 Catalog 与打包报告。\n" +
                "运行时可通过 LoadMode 在 Editor 模拟与真实 AB 之间切换。",
                MessageType.Info);

            DrawRulesPathSection();
            EditorGUILayout.Space(8);
            DrawBasicSection();
            EditorGUILayout.Space(8);
            DrawPackModeSection();
            EditorGUILayout.Space(8);
            DrawCustomRulesSection();
            EditorGUILayout.Space(12);
            DrawActionButtons();
            EditorGUILayout.Space(8);
            DrawReportSection();
            EditorGUILayout.Space(8);
            DrawClearSection();

            EditorGUILayout.EndScrollView();
        }

        void DrawRulesPathSection()
        {
            EditorGUILayout.LabelField("规则 XML", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _rules.RulesXmlPath = EditorGUILayout.TextField("保存路径", _rules.RulesXmlPath);
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                var file = EditorUtility.SaveFilePanelInProject(
                    "保存 ABundle 规则 XML",
                    "ABundleBuildRules",
                    "xml",
                    "选择规则 XML 保存位置",
                    "Assets");
                if (!string.IsNullOrEmpty(file))
                {
                    _rules.RulesXmlPath = file;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("加载 XML"))
            {
                LoadRulesFromDialog();
            }

            if (GUILayout.Button("加载默认"))
            {
                TryLoadDefaultRules();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawBasicSection()
        {
            EditorGUILayout.LabelField("基础配置", EditorStyles.boldLabel);
            _rules.RootFolder = DrawFolderField("资源根目录", _rules.RootFolder);
            _rules.OutputPath = DrawFolderField("AB 输出根目录", _rules.OutputPath);

            var platformOutput = ABundlePathUtility.GetPlatformOutputAssetPath(_rules);
            EditorGUILayout.LabelField("当前平台输出", platformOutput);

            _rules.BundleNamePrefix = EditorGUILayout.TextField("包名前缀", _rules.BundleNamePrefix);
            _rules.BuildTarget = DrawBuildTargetPopup(_rules.BuildTarget);

            var loadIndex = (int)_rules.LoadMode;
            loadIndex = EditorGUILayout.Popup("运行时加载模式", loadIndex, LoadModeLabels);
            _rules.LoadMode = (ABundleLoadMode)loadIndex;

            _rules.GenerateCatalog = EditorGUILayout.Toggle("生成 AssetCatalog.json", _rules.GenerateCatalog);
            _rules.CatalogFileName = EditorGUILayout.TextField("Catalog 文件名", _rules.CatalogFileName);
            _rules.PlatformManifestFileName = EditorGUILayout.TextField("Manifest 包名", _rules.PlatformManifestFileName);

            var locIndex = Array.IndexOf(LocationModeValues, _rules.LocationMode);
            if (locIndex < 0)
            {
                locIndex = 0;
            }

            locIndex = EditorGUILayout.Popup("Location 生成方式", locIndex, LocationModeLabels);
            _rules.LocationMode = LocationModeValues[locIndex];
        }

        void DrawPackModeSection()
        {
            EditorGUILayout.LabelField("分包模式", EditorStyles.boldLabel);
            _rules.PackMode = (ABundlePackMode)EditorGUILayout.EnumPopup("Pack Mode", _rules.PackMode);

            switch (_rules.PackMode)
            {
                case ABundlePackMode.ByTopLevelFolder:
                    EditorGUILayout.HelpBox("根目录下每个一级子文件夹打一个包。", MessageType.None);
                    break;
                case ABundlePackMode.ByDirectoryTree:
                    EditorGUILayout.HelpBox("根目录下每个文件夹（含嵌套）按相对路径打一个包。", MessageType.None);
                    break;
                case ABundlePackMode.SingleRootBundle:
                    EditorGUILayout.HelpBox("根目录下全部资源打进同一个包。", MessageType.None);
                    break;
                case ABundlePackMode.CustomRules:
                    EditorGUILayout.HelpBox("仅使用下方 Custom Rules 列表。", MessageType.None);
                    break;
            }
        }

        void DrawCustomRulesSection()
        {
            if (_rules.PackMode != ABundlePackMode.CustomRules)
            {
                return;
            }

            EditorGUILayout.LabelField("Custom Rules", EditorStyles.boldLabel);
            _rulesScroll = EditorGUILayout.BeginScrollView(_rulesScroll, GUILayout.MaxHeight(160));

            if (_rules.CustomRules == null)
            {
                _rules.CustomRules = new List<ABundleBuildRule>();
            }

            for (var i = 0; i < _rules.CustomRules.Count; i++)
            {
                var rule = _rules.CustomRules[i];
                EditorGUILayout.BeginVertical("box");
                rule.FolderPath = DrawFolderField("文件夹", rule.FolderPath);
                rule.BundleName = EditorGUILayout.TextField("包名", rule.BundleName);
                rule.Recursive = EditorGUILayout.Toggle("递归子目录", rule.Recursive);
                rule.Description = EditorGUILayout.TextField("备注", rule.Description);
                if (GUILayout.Button("删除此规则"))
                {
                    _rules.CustomRules.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("添加规则"))
            {
                _rules.CustomRules.Add(new ABundleBuildRule());
            }
        }

        #endregion

        #region 操作与清除

        void DrawActionButtons()
        {
            EditorGUILayout.LabelField("打包", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存规则 XML", GUILayout.Height(32)))
            {
                SaveRules();
            }

            if (GUILayout.Button("仅打标签", GUILayout.Height(32)))
            {
                BundleLabelApplier.Apply(_rules);
                EditorUtility.DisplayDialog("ABundle", "已根据当前规则打 AssetBundle 标签", "确定");
            }

            if (GUILayout.Button("打包", GUILayout.Height(32)))
            {
                BuildNow();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("保存 XML 并打包", GUILayout.Height(36)))
            {
                if (SaveRules())
                {
                    BuildNow();
                }
            }
        }

        void DrawReportSection()
        {
            _showReport = EditorGUILayout.Foldout(_showReport, "打包报告", true);
            if (!_showReport)
            {
                return;
            }

            if (_lastReport == null)
            {
                var platformPath = ABundlePathUtility.GetPlatformOutputAssetPath(_rules);
                var reportPath = $"{platformPath}/{ABundlePathUtility.GetReportFileName()}";
                if (File.Exists(ABundleRulesXmlIO.ToFullPath(reportPath)))
                {
                    _lastReport = ABundleBuildReporter.LoadReport(reportPath);
                }
            }

            if (_lastReport == null)
            {
                EditorGUILayout.HelpBox("尚无打包报告，请先执行打包。", MessageType.None);
                return;
            }

            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.MaxHeight(200));
            EditorGUILayout.TextArea(ABundleBuildReporter.FormatSummary(_lastReport), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_lastReport.ReportPath) &&
                GUILayout.Button("在 Project 中定位报告"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(_lastReport.ReportPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
        }

        void DrawClearSection()
        {
            EditorGUILayout.LabelField("清除", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("删除规则 XML 和/或打包产物。不可撤销。", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("删除规则 XML", GUILayout.Height(28)))
            {
                ClearRulesXmlOnly();
            }

            if (GUILayout.Button("清空当前平台输出", GUILayout.Height(28)))
            {
                ClearPlatformOutputOnly();
            }

            if (GUILayout.Button("清空全部平台输出", GUILayout.Height(28)))
            {
                ClearAllPlatformOutputs();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("全部清除并重置默认", GUILayout.Height(32)))
            {
                ClearAllAndReset();
            }
        }

        void ClearRulesXmlOnly()
        {
            if (string.IsNullOrWhiteSpace(_rules?.RulesXmlPath))
            {
                EditorUtility.DisplayDialog("ABundle", "请先指定规则 XML 路径", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("清除", $"删除规则 XML？\n{_rules.RulesXmlPath}", "删除", "取消"))
            {
                return;
            }

            var result = ABundleEditorUtility.DeleteRulesXml(_rules.RulesXmlPath);
            EditorUtility.DisplayDialog("ABundle", result.Message, "确定");
        }

        void ClearPlatformOutputOnly()
        {
            var path = ABundlePathUtility.GetPlatformOutputAssetPath(_rules);
            if (!EditorUtility.DisplayDialog("清除", $"清空当前平台输出？\n{path}", "清空", "取消"))
            {
                return;
            }

            var result = ABundleEditorUtility.ClearBuildOutput(path);
            EditorUtility.DisplayDialog("ABundle", result.Message, "确定");
        }

        void ClearAllPlatformOutputs()
        {
            if (!EditorUtility.DisplayDialog(
                    "清除",
                    $"清空输出根目录下所有平台子目录？\n{_rules.OutputPath}",
                    "清空",
                    "取消"))
            {
                return;
            }

            var result = ABundleEditorUtility.ClearAllPlatformOutputs(_rules.OutputPath);
            EditorUtility.DisplayDialog("ABundle", result.Message, "确定");
        }

        void ClearAllAndReset()
        {
            if (!EditorUtility.DisplayDialog(
                    "清除",
                    "删除规则 XML、全部平台输出，并重置为默认配置。是否继续？",
                    "全部清除",
                    "取消"))
            {
                return;
            }

            var result = ABundleEditorUtility.ClearAll(_rules, deleteXml: true, clearOutput: true);
            _rules = ABundleRulesXmlIO.CreateDefault();
            _rules.BuildTarget = ABundlePlatformUtility.FromActiveBuildTarget();
            _lastReport = null;
            EditorUtility.DisplayDialog("ABundle", result.Message + "\n\n已重置为默认配置。", "确定");
        }

        bool SaveRules()
        {
            if (string.IsNullOrWhiteSpace(_rules.RulesXmlPath))
            {
                EditorUtility.DisplayDialog("ABundle", "请先指定规则 XML 保存路径", "确定");
                return false;
            }

            ABundleRulesXmlIO.Save(_rules.RulesXmlPath, _rules);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("ABundle", $"规则已保存:\n{_rules.RulesXmlPath}", "确定");
            return true;
        }

        void BuildNow()
        {
            _lastReport = ABundlePacker.BuildFromRules(_rules);
            var summary = ABundleBuildReporter.FormatSummary(_lastReport);
            EditorUtility.DisplayDialog(
                "ABundle",
                _lastReport.Success ? $"打包成功\n\n{summary}" : $"打包失败\n\n{summary}",
                "确定");
        }

        void TryLoadDefaultRules()
        {
            var path = ABundleRulesXmlIO.DefaultRulesRelativePath;
            if (File.Exists(ABundleRulesXmlIO.ToFullPath(path)))
            {
                _rules = ABundleRulesXmlIO.Load(path);
            }
            else
            {
                _rules = ABundleRulesXmlIO.CreateDefault();
                _rules.BuildTarget = ABundlePlatformUtility.FromActiveBuildTarget();
            }
        }

        void LoadRulesFromDialog()
        {
            var path = EditorUtility.OpenFilePanel("选择规则 XML", "Assets", "xml");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var dataPath = Application.dataPath.Replace('\\', '/');
            path = path.Replace('\\', '/');
            var assetPath = path.StartsWith(dataPath)
                ? "Assets" + path.Substring(dataPath.Length)
                : path;

            _rules = ABundleRulesXmlIO.Load(assetPath);
            if (_rules == null)
            {
                EditorUtility.DisplayDialog("ABundle", "加载失败", "确定");
            }
        }

        #endregion

        #region 工具方法

        static string DrawFolderField(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("选", GUILayout.Width(28)))
            {
                var selected = EditorUtility.OpenFolderPanel(label, "Assets", string.Empty);
                if (!string.IsNullOrEmpty(selected))
                {
                    selected = selected.Replace('\\', '/');
                    var dataPath = Application.dataPath.Replace('\\', '/');
                    if (selected.StartsWith(dataPath))
                    {
                        path = "Assets" + selected.Substring(dataPath.Length);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            return path;
        }

        static string DrawBuildTargetPopup(string current)
        {
            var platform = ABundlePlatformNames.Parse(current);
            var index = (int)platform;
            index = EditorGUILayout.Popup("目标平台", index, ABundlePlatformNames.All);
            return ABundlePlatformNames.All[index];
        }

        #endregion
    }
}
