# Unity 内置编辑器工具编写指南

本文说明如何用 Unity **内置 Editor API**（`UnityEditor`）扩展编辑器：菜单、窗口、Inspector、资源操作等。  
不依赖第三方插件，脚本放在 `Editor` 文件夹即可。

---

## 一、Editor 脚本是什么？

Unity 编辑器本身也是用 C# 写的。你可以在项目中追加 **仅 Editor 下运行** 的脚本，用来：

- 在顶部菜单 / 右键菜单增加功能
- 打开自定义配置窗口
- 定制 Inspector 面板
- 批量处理资源、自动化打包

**运行时游戏代码** 在 `Assembly-CSharp`；**编辑器工具** 在 `Assembly-CSharp-Editor`（或独立 Editor asmdef），打包进玩家包时会被排除。

---

## 二、前置条件

### 2.1 放哪里？

```
Assets/
└── YourModule/
    ├── Runtime/          ← 运行时脚本（可选命名）
    │   └── MyComponent.cs
    └── Editor/           ← 必须叫 Editor，或在 asmdef 里限定 Editor 平台
        └── MyEditorTool.cs
```

规则：

1. 路径中包含 **`Editor`** 文件夹，或
2. `.asmdef` 里 `"includePlatforms": ["Editor"]`

否则 `using UnityEditor;` 会编译报错。

### 2.2 基本结构

Editor 工具**不必**继承 `MonoBehaviour`，常用 **static 类 + 特性**，或继承 **`EditorWindow` / `Editor`**：

```csharp
using UnityEditor;
using UnityEngine;

public static class MyEditorEntry
{
    [MenuItem("MyTools/Hello")]
    static void Hello()
    {
        Debug.Log("Hello Editor");
    }
}
```

### 2.3 两个命名空间

| 命名空间 | 用途 |
|----------|------|
| `UnityEngine` | 通用类型（Vector3、GameObject、Debug） |
| `UnityEditor` | 仅 Editor 可用（MenuItem、AssetDatabase、EditorWindow） |

---

## 三、菜单项：`[MenuItem]`

通过 `[MenuItem("路径")]` 把 **static 方法** 注册为菜单。

### 3.1 顶部菜单栏

```csharp
[MenuItem("MyTools/Do Work")]
static void DoWork() { }
```

显示在 Unity 窗口最上方：`MyTools → Do Work`

路径里每多一段 `/`，多一级子菜单。

### 3.2 子列表（多级菜单）

Unity **没有** 单独的 SubMenu API；**同父路径** 注册多个 `MenuItem` 即可：

```csharp
[MenuItem("MyTools/Build/Windows")]
static void BuildWin() { }

[MenuItem("MyTools/Build/Android")]
static void BuildAndroid() { }

[MenuItem("MyTools/Open Log Folder")]
static void OpenLog() { }
```

效果：

```
MyTools
 ├── Build
 │    ├── Windows
 │    └── Android
 └── Open Log Folder
```

### 3.3 参数说明

```csharp
[MenuItem("路径", isValidateFunction, priority, enabled = true)]
```

| 参数 | 说明 |
|------|------|
| 路径 | 菜单文字，`/` 分级 |
| `isValidateFunction` | `true` 表示这是**校验函数**，不执行逻辑，只返回 bool 决定菜单是否可用 |
| `priority` | 排序，**数字越小越靠上**；相邻项 priority 差 ≥ 11 时中间会出现**分隔线** |
| `enabled` | 是否启用（少用） |

**执行函数 + 校验函数** 成对出现：

```csharp
[MenuItem("MyTools/Process Selected", true)]   // validate
static bool CanProcess()
{
    return Selection.objects.Length > 0;
}

[MenuItem("MyTools/Process Selected", false, 100)]  // execute
static void Process()
{
    foreach (var o in Selection.objects)
        Debug.Log(AssetDatabase.GetAssetPath(o));
}
```

未选中资源时菜单**变灰**。

### 3.4 快捷键

路径末尾加 `%` `#` `&` 等（平台相关）：

```csharp
// Ctrl+Shift+B (Windows) / Cmd+Shift+B (macOS)
[MenuItem("MyTools/Build %#b")]
static void Build() { }
```

| 符号 | 含义 |
|------|------|
| `%` | Ctrl（macOS 为 Cmd） |
| `#` | Shift |
| `&` | Alt |

### 3.5 Project 窗口右键（Assets）

路径以 **`Assets/`** 开头：

```csharp
[MenuItem("Assets/MyTools/Log Path", false, 2000)]
static void LogAssetPath()
{
    Debug.Log(AssetDatabase.GetAssetPath(Selection.activeObject));
}
```

在 Project 面板右键可见。`2000` 左右可排到右键菜单较后位置。

### 3.6 Hierarchy 右键（GameObject）

路径以 **`GameObject/`** 开头：

```csharp
[MenuItem("GameObject/MyTools/Reset Transform", false, 0)]
static void ResetTransform()
{
    foreach (var go in Selection.gameObjects)
    {
        Undo.RecordObject(go.transform, "Reset Transform");
        go.transform.localPosition = Vector3.zero;
    }
}
```

### 3.7 组件右键（CONTEXT）

```csharp
[MenuItem("CONTEXT/Rigidbody/Log Mass")]
static void LogMass(MenuCommand cmd)
{
    var rb = cmd.context as Rigidbody;
    Debug.Log(rb.mass);
}
```

在 Inspector 里某组件的 **⋮** 或右键菜单中出现。

---

## 四、自定义窗口：`EditorWindow`

需要表单、按钮、可配置参数时，用 **`EditorWindow`** 画 UI。

### 4.1 最小示例

```csharp
using UnityEditor;
using UnityEngine;

public class MyToolWindow : EditorWindow
{
    string inputText = "hello";
    int count = 1;

    [MenuItem("MyTools/My Window...")]
    static void Open()
    {
        // 打开或聚焦已有窗口
        var w = GetWindow<MyToolWindow>("My Tool");
        w.minSize = new Vector2(320, 200);
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("示例工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        inputText = EditorGUILayout.TextField("文本", inputText);
        count = EditorGUILayout.IntField("数量", count);

        if (GUILayout.Button("执行", GUILayout.Height(28)))
        {
            Debug.Log($"{inputText} x {count}");
            ShowNotification(new GUIContent("完成"));
        }
    }
}
```

### 4.2 生命周期

| 回调 | 时机 |
|------|------|
| `OnEnable` | 窗口打开 / 脚本重编译后 |
| `OnDisable` | 窗口关闭 |
| `OnGUI` | 每帧重绘 UI（IMGUI） |
| `OnFocus` / `OnLostFocus` | 焦点变化 |

### 4.3 常用 IMGUI 控件

```csharp
// 文本
EditorGUILayout.TextField("标签", value);
EditorGUILayout.IntField("整数", n);
EditorGUILayout.FloatField("浮点", f);
EditorGUILayout.Toggle("开关", flag);

// 枚举
myEnum = (MyEnum)EditorGUILayout.EnumPopup("模式", myEnum);
flags = (MyFlags)EditorGUILayout.EnumFlagsField("多选", flags);

// 资源引用
prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

// 布局
EditorGUILayout.BeginHorizontal();
// ...
EditorGUILayout.EndHorizontal();

EditorGUILayout.Space();
EditorGUILayout.HelpBox("提示信息", MessageType.Info);

// 按钮
if (GUILayout.Button("确定")) { }
```

### 4.4 长任务与反馈

```csharp
try
{
    for (int i = 0; i < 100; i++)
    {
        if (EditorUtility.DisplayCancelableProgressBar("处理中", $"进度 {i}%", i / 100f))
            break;
        // 耗时操作...
    }
}
finally
{
    EditorUtility.ClearProgressBar();
}

EditorUtility.DisplayDialog("标题", "操作完成", "确定");
```

### 4.5 UI Toolkit（可选，Unity 2021+）

新项目也可用 **UI Toolkit**（`.uxml` + `.uss`）替代 IMGUI：

```csharp
public class MyUIToolWindow : EditorWindow
{
    [MenuItem("MyTools/UI Toolkit Window")]
    static void Open() => GetWindow<MyUIToolWindow>();

    void CreateGUI()
    {
        var root = rootVisualElement;
        var label = new Label("Hello UI Toolkit");
        root.Add(label);
    }
}
```

入门阶段 **IMGUI（OnGUI）** 资料更多，与本文示例一致。

---

## 五、自定义 Inspector：`[CustomEditor]`

改写某组件在 Inspector 里的显示方式。

```csharp
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 保留默认字段
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var comp = (MyComponent)target;
        if (GUILayout.Button("在 Scene 中标记"))
        {
            Debug.Log(comp.name);
        }
    }
}
```

若组件有 `[CustomEditor(typeof(MyComponent), true)]` 的第二个参数 `true`，则对 **子类** 也生效。

### 5.1 推荐：SerializedObject

直接改 `target` 字段不利于 Undo。标准写法：

```csharp
SerializedProperty hpProp;

void OnEnable()
{
    hpProp = serializedObject.FindProperty("hp");
}

public override void OnInspectorGUI()
{
    serializedObject.Update();
    EditorGUILayout.PropertyField(hpProp);
    serializedObject.ApplyModifiedProperties();
}
```

自动支持 **Undo** 和 **Prefab 覆盖**。

---

## 六、属性绘制器

### 6.1 `[CustomPropertyDrawer]` — 单个字段 UI

```csharp
[CustomPropertyDrawer(typeof(MyAttribute))]
public class MyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
    {
        EditorGUI.PropertyField(rect, prop, label);
    }
}
```

### 6.2 `[CustomPropertyDrawer]` + `[PropertyAttribute]`

给字段加特性，在 Inspector 里定制显示（范围滑条、只读、下拉等）。

---

## 七、资源与场景：`AssetDatabase`

Editor 里读写 `Assets/` 下资源的核心 API。

| API | 作用 |
|-----|------|
| `AssetDatabase.GetAssetPath(obj)` | 对象 → 路径 |
| `AssetDatabase.LoadAssetAtPath<T>(path)` | 路径 → 对象 |
| `AssetDatabase.Refresh()` | 重新扫描磁盘，更新 Project 窗口 |
| `AssetDatabase.CreateAsset(obj, path)` | 创建 ScriptableObject 等资源文件 |
| `AssetDatabase.SaveAssets()` | 保存未写入磁盘的修改 |
| `AssetDatabase.FindAssets("t:Prefab", folders)` | 按类型搜索 |
| `AssetDatabase.GetDependencies(path)` | 查依赖 |

修改资源后常见组合：

```csharp
EditorUtility.SetDirty(myAsset);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

---

## 八、Undo 与 Prefab

对用户可见的修改应支持 **撤销**：

```csharp
Undo.RecordObject(targetObject, "修改说明");
targetObject.someField = newValue;
EditorUtility.SetDirty(targetObject);
```

Prefab 相关：

```csharp
PrefabUtility.IsPartOfPrefabAsset(obj);
PrefabUtility.SavePrefabAsset(prefabRoot);
```

---

## 九、Selection 与 EditorUtility

| API | 作用 |
|-----|------|
| `Selection.activeObject` | 当前选中单个对象 |
| `Selection.objects` | 当前选中多个 |
| `Selection.activeGameObject` | Hierarchy 选中 GO |
| `EditorUtility.OpenFolderPanel(...)` | 系统文件夹选择对话框 |
| `EditorUtility.OpenFilePanel(...)` | 系统文件选择对话框 |
| `EditorUtility.RevealInFinder(path)` | 在资源管理器中定位文件 |
| `EditorUtility.DisplayDialog(...)` | 模态提示框 |

---

## 十、工具类型对照表

| 你想做什么 | 用什么 | 入口 |
|------------|--------|------|
| 顶部菜单点一下执行 | `[MenuItem]` + static 方法 | `"MyTools/..."` |
| 多级子菜单 | 多个 `[MenuItem]` 同前缀 | `"A/B/C"` |
| 菜单置灰 | validate 函数 | 第二参数 `true` |
| Project 右键 | `[MenuItem]` | `"Assets/..."` |
| Hierarchy 右键 | `[MenuItem]` | `"GameObject/..."` |
| 组件右键 | `[MenuItem]` | `"CONTEXT/组件名/..."` |
| 独立配置面板 | `EditorWindow` + `OnGUI` | `[MenuItem]` 里 `GetWindow<>()` |
| 改 Inspector 外观 | `[CustomEditor]` | 绑定 `typeof(组件)` |
| 改单个字段 UI | `[CustomPropertyDrawer]` | 绑定 `PropertyAttribute` |
| 批量改资源 | `AssetDatabase` + 菜单触发 | 任意入口 |

---

## 十一、推荐目录结构

```
YourModule/
├── Editor/
│   ├── Menus/              MenuItem 入口
│   ├── Windows/            EditorWindow
│   ├── Inspectors/         CustomEditor
│   ├── Drawers/            PropertyDrawer
│   └── Utilities/          纯静态辅助（AssetDatabase 封装等）
└── Runtime/
    └── ...
```

一个功能一种入口：菜单只负责打开窗口或调用 Pipeline，复杂逻辑放在独立类里，便于测试和维护。

---

## 十二、完整小例子：菜单 + 窗口 + 资源操作

```csharp
// File: Assets/MyGame/Editor/Windows/RenameToolWindow.cs
using System.IO;
using UnityEditor;
using UnityEngine;

public class RenameToolWindow : EditorWindow
{
    string suffix = "_v1";

    [MenuItem("MyTools/Rename Tool...")]
    static void Open() => GetWindow<RenameToolWindow>("Rename Tool");

    [MenuItem("Assets/MyTools/Append Suffix", true)]
    static bool Validate() => Selection.objects.Length > 0;

    [MenuItem("Assets/MyTools/Append Suffix")]
    static void OpenFromContext()
    {
        var w = GetWindow<RenameToolWindow>("Rename Tool");
        w.Show();
    }

    void OnGUI()
    {
        suffix = EditorGUILayout.TextField("后缀", suffix);
        if (GUILayout.Button("给选中资源重命名"))
            ApplyRename();
    }

    void ApplyRename()
    {
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            var err = AssetDatabase.RenameAsset(path, Path.GetFileNameWithoutExtension(path) + suffix);
            if (!string.IsNullOrEmpty(err))
                Debug.LogError(err);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
```

同一功能：**顶部菜单** 打开窗口，**Project 右键** 也可打开。

---

## 十三、附录：本项目 AB 打包示例

`Assets/Test/ResTest/Editor/AssetEditorTest.cs` 是上述模式的极简应用：

```csharp
[MenuItem("vFramework/Build Test AB")]
public static void Build()
{
    Directory.CreateDirectory(OutputPath);
    BuildPipeline.BuildAssetBundles(
        OutputPath,
        BuildAssetBundleOptions.None,
        EditorUserBuildSettings.activeBuildTarget);
    AssetDatabase.Refresh();
}
```

| API | 作用 |
|-----|------|
| `BuildPipeline.BuildAssetBundles` | Unity 内置 AB 打包 |
| 参数 1 `outputPath` | 输出目录 |
| 参数 2 `options` | 压缩、强制重建等 |
| 参数 3 `targetPlatform` | 目标平台，常用 `EditorUserBuildSettings.activeBuildTarget` |

更完整的 Editor 写法见本文第三～四章；AB 学习路线见 `Assets/vFramework/BaseLayer/AssetLayer/方案对比与学习指南.md`。

---

## 十四、官方文档

- [Unity Manual: Editor Scripting](https://docs.unity3d.com/Manual/EditorScripting.html)
- [MenuItem](https://docs.unity3d.com/ScriptReference/MenuItem.html)
- [EditorWindow](https://docs.unity3d.com/ScriptReference/EditorWindow.html)
- [CustomEditor](https://docs.unity3d.com/ScriptReference/CustomEditor.html)
- [AssetDatabase](https://docs.unity3d.com/ScriptReference/AssetDatabase.html)

---

*文档版本：Unity 内置 Editor 工具编写指南*
