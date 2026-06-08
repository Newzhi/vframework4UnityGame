using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BundleRuleMaker : EditorWindow
{
    #region 变量定义

    const string WindowTitle = "AssetBundle Packer";
    const string DefaultSettingPath = BundleBuilder.DefaultSettingPath;

    BuildSetting setting;
    Vector2 scrollPos;

    static readonly string[] BuildModeLabels =
    {
        "编辑器测试", "真机环境", "CDN联网"
    };

    static readonly BuildMode[] BuildModes =
    {
        BuildMode.EditorTest,
        BuildMode.DeviceDebug,
        BuildMode.CdnHotUpdate,
    };

    static readonly string[] RuleLabels =
    {
        "默认打包 - 按第一级子文件夹打包",
        "细化打包 - 按所有子文件夹打包",
        "自定义打包 - 手动配置每个资源",
    };

    #endregion

    #region Unity编辑器顶部的工具调用呼出菜单

    [MenuItem("Test/AssetBundle Packer")]
    static void OpenWindow()
    {
        BundleRuleMaker window = GetWindow<BundleRuleMaker>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(720, 640);
        window.Show();
    }

    #endregion

    #region 窗口生命周期

    void OnEnable()
    {
        LoadOrCreateSetting();
    }

    void OnGUI()
    {
        if (setting == null)
            LoadOrCreateSetting();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawBasicSettings();
        EditorGUILayout.Space(8);
        DrawRuleConfig();

        if (setting.packingRule == PackingRule.Custom)
        {
            EditorGUILayout.Space(8);
            DrawCustomConfigList();
        }

        EditorGUILayout.Space(8);
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region UI绘制

    void DrawBasicSettings()
    {
        EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        setting.platform = (BuildPlatform)EditorGUILayout.EnumPopup("目标平台", setting.platform);
        setting.version = EditorGUILayout.TextField("版本号", setting.version);
        setting.buildNumber = EditorGUILayout.IntField("构建号", setting.buildNumber);

        DrawOutputPathField("真机环境输出路径", ref setting.deviceOutputPath);
        DrawOutputPathField("联网 CDN 输出路径", ref setting.cdnOutputPath);

        if (GUILayout.Button("更新版本号（patch+1 / build+1）"))
            BumpVersion();

        EditorGUI.indentLevel--;
    }

    void DrawOutputPathField(string label, ref string pathField)
    {
        EditorGUILayout.BeginHorizontal();
        pathField = EditorGUILayout.TextField(label, pathField);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string abs = EditorUtility.OpenFolderPanel(
                "选择输出目录",
                BundleBuilder.ToAbsoluteAssetsPath(pathField),
                "");
            if (!string.IsNullOrEmpty(abs))
            {
                string relative = BundleBuilder.ToAssetsRelativePath(abs);
                if (!string.IsNullOrEmpty(relative))
                    pathField = relative;
                else
                    pathField = abs;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawRuleConfig()
    {
        EditorGUILayout.LabelField("打包规则配置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        int ruleIndex = (int)setting.packingRule;
        int newRuleIndex = EditorGUILayout.Popup("打包规则", ruleIndex, RuleLabels);
        PackingRule newRule = (PackingRule)newRuleIndex;
        if (newRuleIndex != ruleIndex && newRule != PackingRule.Custom)
            setting.buildMode = BuildMode.DeviceDebug;
        setting.packingRule = newRule;

        if (setting.packingRule != PackingRule.Custom)
        {
            EditorGUILayout.HelpBox(
                "默认打包 / 细化打包的全局打包模式默认为「真机环境」，可按需改为编辑器测试或 CDN联网。",
                MessageType.None);

            int modeIndex = Array.IndexOf(BuildModes, setting.buildMode);
            if (modeIndex < 0)
                modeIndex = 0;
            int newModeIndex = EditorGUILayout.Popup("打包模式", modeIndex, BuildModeLabels);
            setting.buildMode = BuildModes[newModeIndex];
            EditorGUILayout.HelpBox(GetBuildModeDescription(setting.buildMode), MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        setting.targetDirectory = EditorGUILayout.TextField("目标资源目录", setting.targetDirectory);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string abs = EditorUtility.OpenFolderPanel(
                "选择资源根目录",
                BundleBuilder.ToAbsoluteAssetsPath(setting.targetDirectory),
                "");
            string relative = BundleBuilder.ToAssetsRelativePath(abs);
            if (!string.IsNullOrEmpty(relative))
                setting.targetDirectory = relative;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(GetRuleDescription(setting.packingRule), MessageType.Info);
        EditorGUI.indentLevel--;
    }

    void DrawCustomConfigList()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("资源打包配置", EditorStyles.boldLabel);
        if (GUILayout.Button("+ 添加配置", GUILayout.Width(100)))
        {
            setting.customItems.Add(new BundleConfigItem
            {
                assetPath = setting.targetDirectory,
                bundleName = "bundle_" + (setting.customItems.Count + 1),
                buildMode = setting.buildMode
            });
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < setting.customItems.Count; i++)
        {
            BundleConfigItem item = setting.customItems[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("配置项 #" + (i + 1), EditorStyles.boldLabel);
            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                setting.customItems.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            item.assetPath = EditorGUILayout.TextField("资源路径", item.assetPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string abs = EditorUtility.OpenFolderPanel(
                    "选择资源路径",
                    BundleBuilder.ToAbsoluteAssetsPath(item.assetPath),
                    "");
                string relative = BundleBuilder.ToAssetsRelativePath(abs);
                if (!string.IsNullOrEmpty(relative))
                    item.assetPath = relative;
            }
            EditorGUILayout.EndHorizontal();

            item.bundleName = EditorGUILayout.TextField("包名 (Bundle Name)", item.bundleName);

            int itemModeIndex = Array.IndexOf(BuildModes, item.buildMode);
            if (itemModeIndex < 0)
                itemModeIndex = 0;
            int newItemModeIndex = EditorGUILayout.Popup("打包模式", itemModeIndex, BuildModeLabels);
            item.buildMode = BuildModes[newItemModeIndex];

            item.downloadPriority = (DownloadPriority)EditorGUILayout.EnumPopup("下载优先级", item.downloadPriority);
            item.resourceCategory = (ResourceCategory)EditorGUILayout.EnumPopup("资源类型", item.resourceCategory);
            item.note = EditorGUILayout.TextField("备注说明", item.note);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("清理打包", GUILayout.Width(100), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("清理打包", "确定清理输出目录中的 bundle 与清单？", "确定", "取消"))
                BundleBuilder.Clean(setting);
        }

        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.9f);
        if (GUILayout.Button("开始打包", GUILayout.Width(100), GUILayout.Height(30)))
        {
            SaveSetting();
            BundleBuilder.Build(setting);
        }

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("保存规则", GUILayout.Width(100), GUILayout.Height(30)))
            SaveSetting();

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 辅助函数

    void LoadOrCreateSetting()
    {
        setting = AssetDatabase.LoadAssetAtPath<BuildSetting>(DefaultSettingPath);
        if (setting == null)
        {
            EnsureAssetFolder(Path.GetDirectoryName(DefaultSettingPath).Replace("\\", "/"));
            setting = CreateInstance<BuildSetting>();
            AssetDatabase.CreateAsset(setting, DefaultSettingPath);
            AssetDatabase.SaveAssets();
        }

        EnsureDefaultPaths();
    }

    void EnsureDefaultPaths()
    {
        if (string.IsNullOrEmpty(setting.deviceOutputPath))
            setting.deviceOutputPath = "Assets/StreamingAssets";
        if (string.IsNullOrEmpty(setting.cdnOutputPath))
            setting.cdnOutputPath = "Bundles/CDN";
        if (string.IsNullOrEmpty(setting.targetDirectory))
            setting.targetDirectory = "Assets/Test/AB_Test_Target";
    }

    static void EnsureAssetFolder(string assetsFolder)
    {
        if (AssetDatabase.IsValidFolder(assetsFolder))
            return;

        string parent = Path.GetDirectoryName(assetsFolder).Replace("\\", "/");
        string folderName = Path.GetFileName(assetsFolder);
        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    void SaveSetting()
    {
        if (setting == null)
            return;

        EditorUtility.SetDirty(setting);
        AssetDatabase.SaveAssets();
        Debug.Log("打包规则已保存: " + DefaultSettingPath);
    }

    void BumpVersion()
    {
        string[] parts = setting.version.Split('.');
        if (parts.Length < 3)
        {
            Debug.LogWarning("版本号格式应为 x.y.z");
            return;
        }

        int patch = int.TryParse(parts[2], out int p) ? p : 0;
        parts[2] = (patch + 1).ToString();
        setting.version = string.Join(".", parts);
        setting.buildNumber++;
    }

    static string GetRuleDescription(PackingRule rule)
    {
        switch (rule)
        {
            case PackingRule.Detailed:
                return "细化打包：按照指定目录下每一个子文件夹（包括嵌套文件夹）作为一个 AB 包。全局打包模式默认为「真机环境」。";
            case PackingRule.Custom:
                return "自定义打包：每项配置的打包模式决定 AB 输出位置，可自由添加配置项。";
            default:
                return "默认打包：按照指定目录下每一个第一级子文件夹作为一个 AB 包。全局打包模式默认为「真机环境」。";
        }
    }

    static string GetBuildModeDescription(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.DeviceDebug:
                return "真机环境：AB 输出到真机环境输出路径（默认 StreamingAssets）。";
            case BuildMode.CdnHotUpdate:
                return "CDN联网：AB 输出到联网 CDN 输出路径（默认 Bundles/CDN）。";
            default:
                return "编辑器测试：不生成 .bundle，仅更新清单（模拟阶段）。";
        }
    }

    #endregion
}
