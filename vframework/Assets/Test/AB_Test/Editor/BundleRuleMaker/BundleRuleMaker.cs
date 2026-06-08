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
        "编辑器测试", "真机模式/首包", "CDN联网"
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

        setting.platform = (BuildPlatform)EditorGUILayout.EnumPopup(
            Tip("目标平台", "构建目标平台，决定 BuildPipeline 使用的 BuildTarget。"),
            setting.platform);
        setting.version = EditorGUILayout.TextField(
            Tip("版本号", "应用版本号（x.y.z），写入 AssetCatalog.json 的 version 字段。"),
            setting.version);
        setting.buildNumber = EditorGUILayout.IntField(
            Tip("构建号", "递增构建编号，写入 AssetCatalog.json 的 buildNumber 字段。"),
            setting.buildNumber);

        DrawOutputPathField(
            "首包输出路径",
            "真机模式/首包下 AB 包的输出目录，默认 Assets/StreamingAssets。",
            ref setting.deviceOutputPath);
        DrawOutputPathField(
            "联网 CDN 输出路径",
            "CDN联网模式下 AB 包的输出目录，默认 Bundles/CDN（项目根相对路径）。",
            ref setting.cdnOutputPath);

        if (GUILayout.Button(Tip("更新版本号（patch+1 / build+1）", "版本号 patch 位 +1，同时 buildNumber +1。")))
            BumpVersion();

        EditorGUI.indentLevel--;
    }

    void DrawOutputPathField(string label, string tooltip, ref string pathField)
    {
        EditorGUILayout.BeginHorizontal();
        pathField = EditorGUILayout.TextField(Tip(label, tooltip), pathField);
        if (GUILayout.Button(Tip("浏览", "打开文件夹选择对话框，选择输出目录。"), GUILayout.Width(60)))
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
        int newRuleIndex = EditorGUILayout.Popup(
            Tip("打包规则", GetRuleDescription((PackingRule)ruleIndex)),
            ruleIndex,
            RuleLabels);
        PackingRule newRule = (PackingRule)newRuleIndex;
        if (newRuleIndex != ruleIndex && newRule != PackingRule.Custom)
            setting.buildMode = BuildMode.DeviceDebug;
        setting.packingRule = newRule;

        if (setting.packingRule != PackingRule.Custom)
        {
            int modeIndex = Array.IndexOf(BuildModes, setting.buildMode);
            if (modeIndex < 0)
                modeIndex = 0;
            int newModeIndex = EditorGUILayout.Popup(
                Tip("打包模式", GetBuildModeFieldTooltip(setting.buildMode)),
                modeIndex,
                BuildModeLabels);
            setting.buildMode = BuildModes[newModeIndex];
        }

        EditorGUILayout.BeginHorizontal();
        setting.targetDirectory = EditorGUILayout.TextField(
            Tip("目标资源目录", "Default / Detailed 规则下扫描并打包资源的根目录。"),
            setting.targetDirectory);
        if (GUILayout.Button(Tip("浏览", "打开文件夹选择对话框，选择资源根目录。"), GUILayout.Width(60)))
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

        EditorGUI.indentLevel--;
    }

    void DrawCustomConfigList()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            Tip("资源打包配置", "自定义打包：每项配置的打包模式决定 AB 输出位置，可自由添加配置项。"),
            EditorStyles.boldLabel);
        if (GUILayout.Button(Tip("+ 添加配置", "新增一条自定义打包配置项。"), GUILayout.Width(100)))
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
            if (GUILayout.Button(Tip("删除", "移除此配置项。"), GUILayout.Width(60)))
            {
                setting.customItems.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            item.assetPath = EditorGUILayout.TextField(
                Tip("资源路径", "文件夹路径：打包该目录下全部资源；单文件路径：仅打包该资源。"),
                item.assetPath);
            if (GUILayout.Button(Tip("浏览", "选择资源文件夹或单个资源。"), GUILayout.Width(60)))
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

            item.bundleName = EditorGUILayout.TextField(
                Tip("包名 (Bundle Name)", "AssetBundle 名称，无需手动加 .bundle 后缀。"),
                item.bundleName);

            int itemModeIndex = Array.IndexOf(BuildModes, item.buildMode);
            if (itemModeIndex < 0)
                itemModeIndex = 0;
            int newItemModeIndex = EditorGUILayout.Popup(
                Tip("打包模式", GetBuildModeDescription(item.buildMode)),
                itemModeIndex,
                BuildModeLabels);
            item.buildMode = BuildModes[newItemModeIndex];

            item.downloadPriority = (DownloadPriority)EditorGUILayout.EnumPopup(
                Tip("下载优先级", "资源下载优先级标记，供后续热更策略使用。"),
                item.downloadPriority);
            item.resourceCategory = (ResourceCategory)EditorGUILayout.EnumPopup(
                Tip("资源类型", "资源分类标记，便于管理与筛选。"),
                item.resourceCategory);
            item.note = EditorGUILayout.TextField(
                Tip("备注说明", "可选的配置说明，仅用于编辑器内标注。"),
                item.note);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button(Tip("清理打包", "清理首包/CDN 输出目录中的 bundle、manifest 与 Catalogue。"), GUILayout.Width(100), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("清理打包", "确定清理输出目录中的 bundle 与清单？", "确定", "取消"))
                BundleBuilder.Clean(setting);
        }

        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.9f);
        if (GUILayout.Button(Tip("开始打包", "按当前规则与打包模式执行 BuildPipeline 并生成清单。"), GUILayout.Width(100), GUILayout.Height(30)))
        {
            SaveSetting();
            BundleBuilder.Build(setting);
        }

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button(Tip("保存规则", "将当前配置写入 DefaultBuildSetting.asset。"), GUILayout.Width(100), GUILayout.Height(30)))
            SaveSetting();

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 辅助函数

    static GUIContent Tip(string label, string tooltip) => new GUIContent(label, tooltip);

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
                return "细化打包：指定目录下每一个子文件夹（含嵌套）各打一个 AB 包。全局打包模式默认为「真机模式/首包」。";
            case PackingRule.Custom:
                return "自定义打包：每项配置的打包模式决定 AB 输出位置，可自由添加配置项。";
            default:
                return "默认打包：指定目录下每个第一级子文件夹各打一个 AB 包。全局打包模式默认为「真机模式/首包」。";
        }
    }

    static string GetBuildModeFieldTooltip(BuildMode mode)
    {
        return "默认打包 / 细化打包时默认为「真机模式/首包」，可按需切换。\n" + GetBuildModeDescription(mode);
    }

    static string GetBuildModeDescription(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.DeviceDebug:
                return "真机模式/首包：AB 输出到首包输出路径（默认 StreamingAssets）。";
            case BuildMode.CdnHotUpdate:
                return "CDN联网：AB 输出到联网 CDN 输出路径（默认 Bundles/CDN）。";
            default:
                return "编辑器测试：不生成 .bundle，仅更新清单（模拟阶段）。";
        }
    }

    #endregion
}
