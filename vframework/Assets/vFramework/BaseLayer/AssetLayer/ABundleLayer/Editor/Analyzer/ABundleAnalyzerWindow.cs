// ABundleAnalyzerWindow.cs — 辅助工具窗口（Editor / Analyzer，暂未实现）
// 用途：依赖分析器 UI 占位，保留菜单入口与按钮布局，逻辑待后续实现。
// 菜单：vFramework → AssetKit → ABundleAnalyzer

using UnityEditor;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    public class ABundleAnalyzerWindow : EditorWindow
    {
        string _searchQuery = string.Empty;
        string _locationQuery = string.Empty;
        int _platformIndex;

        [MenuItem("vFramework/AssetKit/ABundleAnalyzer")]
        public static void Open()
        {
            var window = GetWindow<ABundleAnalyzerWindow>("ABundle Analyzer");
            window.minSize = new Vector2(640, 480);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("ABundle Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "分析器暂未实现。以下按钮为占位，后续将支持 Manifest / Catalog 依赖查询。",
                MessageType.Info);

            EditorGUILayout.Space(8);
            DrawSourceSection();
            EditorGUILayout.Space(8);
            DrawSearchSection();
            EditorGUILayout.Space(8);
            DrawMainPanels();
        }

        void DrawSourceSection()
        {
            EditorGUILayout.LabelField("数据源", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从默认规则加载", GUILayout.Height(24)))
            {
            }

            if (GUILayout.Button("选择规则 XML", GUILayout.Height(24)))
            {
            }

            if (GUILayout.Button("刷新", GUILayout.Height(24)))
            {
            }

            EditorGUILayout.EndHorizontal();

            _platformIndex = EditorGUILayout.Popup(
                "平台",
                _platformIndex,
                ABundlePlatformNames.All);

            EditorGUILayout.LabelField("分析目录", "（暂未实现）");
        }

        void DrawSearchSection()
        {
            EditorGUILayout.BeginHorizontal();
            _searchQuery = EditorGUILayout.TextField("包名过滤", _searchQuery);
            if (GUILayout.Button("清空", GUILayout.Width(48)))
            {
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _locationQuery = EditorGUILayout.TextField("Location 查询", _locationQuery);
            if (GUILayout.Button("查", GUILayout.Width(48)))
            {
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawMainPanels()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(240));
            EditorGUILayout.LabelField("包列表", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("（暂未实现）", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("详情", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("请从左侧选择一个包。（暂未实现）", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
    }
}
