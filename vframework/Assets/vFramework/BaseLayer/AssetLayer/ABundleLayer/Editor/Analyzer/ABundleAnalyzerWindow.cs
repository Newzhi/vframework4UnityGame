using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    /// <summary>辅助工具：Manifest / Catalog 依赖分析。菜单：vFramework → AssetKit → ABundleAnalyzer</summary>
    public class ABundleAnalyzerWindow : EditorWindow
    {
        #region 字段

        readonly ABundleAnalyzer _analyzer = new();

        ABundleBuildRules _rules;
        string _selectedBundle;
        string _searchQuery = string.Empty;
        string _locationQuery = string.Empty;
        Vector2 _bundleScroll;
        Vector2 _detailScroll;
        int _platformIndex;

        #endregion

        #region 菜单入口

        [MenuItem("vFramework/AssetKit/ABundleAnalyzer")]
        public static void Open()
        {
            var window = GetWindow<ABundleAnalyzerWindow>("ABundle Analyzer");
            window.minSize = new Vector2(640, 480);
            window.Show();
        }

        void OnEnable()
        {
            if (_rules == null)
            {
                LoadDefaultRules();
            }

            ReloadAnalyzer();
        }

        #endregion

        #region 界面绘制

        void OnGUI()
        {
            EditorGUILayout.LabelField("ABundle Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "读取已打包 Manifest / Catalog，查询包依赖、被依赖关系与 Location 索引。",
                MessageType.Info);

            DrawSourceSection();
            EditorGUILayout.Space(8);

            if (!_analyzer.IsLoaded)
            {
                EditorGUILayout.HelpBox("未加载有效 Manifest。请先打包或检查平台输出目录。", MessageType.Warning);
                return;
            }

            DrawSearchSection();
            EditorGUILayout.Space(4);
            DrawMainPanels();
        }

        void DrawSourceSection()
        {
            EditorGUILayout.LabelField("数据源", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从默认规则加载", GUILayout.Height(24)))
            {
                LoadDefaultRules();
                ReloadAnalyzer();
            }

            if (GUILayout.Button("选择规则 XML", GUILayout.Height(24)))
            {
                LoadRulesFromDialog();
                ReloadAnalyzer();
            }

            if (GUILayout.Button("刷新", GUILayout.Height(24)))
            {
                ReloadAnalyzer();
            }

            EditorGUILayout.EndHorizontal();

            _platformIndex = EditorGUILayout.Popup(
                "平台",
                _platformIndex,
                ABundlePlatformNames.All);

            if (_rules != null)
            {
                _rules.BuildTarget = ABundlePlatformNames.All[_platformIndex];
            }

            var output = _rules != null
                ? ABundlePathUtility.GetPlatformOutputAssetPath(_rules)
                : string.Empty;
            EditorGUILayout.LabelField("分析目录", output);
        }

        void DrawSearchSection()
        {
            EditorGUILayout.BeginHorizontal();
            _searchQuery = EditorGUILayout.TextField("包名过滤", _searchQuery);
            if (GUILayout.Button("清空", GUILayout.Width(48)))
            {
                _searchQuery = string.Empty;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _locationQuery = EditorGUILayout.TextField("Location 查询", _locationQuery);
            if (GUILayout.Button("查", GUILayout.Width(48)))
            {
                QueryLocation();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawMainPanels()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧包列表
            EditorGUILayout.BeginVertical(GUILayout.Width(240));
            EditorGUILayout.LabelField($"包列表 ({_analyzer.BundleNames.Count})", EditorStyles.boldLabel);
            _bundleScroll = EditorGUILayout.BeginScrollView(_bundleScroll, GUILayout.ExpandHeight(true));

            var bundles = _analyzer.BundleNames
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Where(n => string.IsNullOrEmpty(_searchQuery) ||
                            n.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var name in bundles)
            {
                var sizeKb = _analyzer.GetBundleSize(name) / 1024f;
                var label = $"{name}  ({sizeKb:F1} KB)";
                if (GUILayout.Toggle(name == _selectedBundle, label, "Button"))
                {
                    _selectedBundle = name;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // 右侧详情
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("详情", EditorStyles.boldLabel);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));
            DrawBundleDetail(_selectedBundle);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        void DrawBundleDetail(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                EditorGUILayout.HelpBox("请从左侧选择一个包。", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("包名", bundleName);
            EditorGUILayout.LabelField("大小", FormatSize(_analyzer.GetBundleSize(bundleName)));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("依赖（此包需要）", EditorStyles.boldLabel);
            DrawStringList(_analyzer.GetDependencies(bundleName));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("被依赖（依赖此包的包）", EditorStyles.boldLabel);
            DrawStringList(_analyzer.GetDependents(bundleName));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("推荐加载顺序", EditorStyles.boldLabel);
            DrawStringList(_analyzer.GetLoadOrder(bundleName));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Catalog Locations", EditorStyles.boldLabel);
            var locations = _analyzer.FindLocationsByBundle(bundleName);
            if (locations.Count == 0)
            {
                EditorGUILayout.LabelField("（无）");
            }
            else
            {
                foreach (var loc in locations.Take(20))
                {
                    EditorGUILayout.LabelField("·", $"{loc.Location}  ←  {loc.AssetName}");
                }

                if (locations.Count > 20)
                {
                    EditorGUILayout.LabelField($"… 共 {locations.Count} 条");
                }
            }
        }

        void DrawStringList(System.Collections.Generic.IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
            {
                EditorGUILayout.LabelField("（无）");
                return;
            }

            foreach (var item in items)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("·", item);
                if (GUILayout.Button("→", GUILayout.Width(24)))
                {
                    _selectedBundle = item;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void QueryLocation()
        {
            if (string.IsNullOrWhiteSpace(_locationQuery))
            {
                return;
            }

            if (_analyzer.TryFindLocation(_locationQuery.Trim(), out var entry))
            {
                _selectedBundle = entry.BundleName;
                EditorUtility.DisplayDialog(
                    "Location",
                    $"Location: {entry.Location}\n" +
                    $"Bundle: {entry.BundleName}\n" +
                    $"Asset: {entry.AssetName}\n" +
                    $"Type: {entry.AssetType}",
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("Location", $"未找到: {_locationQuery}", "确定");
            }
        }

        #endregion

        #region 数据加载

        void LoadDefaultRules()
        {
            var path = ABundleRulesXmlIO.DefaultRulesRelativePath;
            if (System.IO.File.Exists(ABundleRulesXmlIO.ToFullPath(path)))
            {
                _rules = ABundleRulesXmlIO.Load(path);
            }
            else
            {
                _rules = ABundleRulesXmlIO.CreateDefault();
            }

            _platformIndex = (int)ABundlePlatformNames.Parse(_rules.BuildTarget);
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
            if (_rules != null)
            {
                _platformIndex = (int)ABundlePlatformNames.Parse(_rules.BuildTarget);
            }
        }

        void ReloadAnalyzer()
        {
            _analyzer.Clear();
            if (_rules == null)
            {
                return;
            }

            _rules.BuildTarget = ABundlePlatformNames.All[_platformIndex];
            if (!_analyzer.LoadFromRules(_rules))
            {
                Debug.LogWarning("[ABundleAnalyzer] 加载失败，请确认已对该平台执行打包。");
            }
        }

        static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024f * 1024f):F2} MB";
            }

            return $"{bytes / 1024f:F1} KB";
        }

        #endregion
    }
}
