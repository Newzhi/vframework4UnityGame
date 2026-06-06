# AB 学习演示：三个问题的答案与场景绑定

## 使用前（Editor）

1. 菜单 **vFramework → AB Demo → Apply Demo AB Labels**（写入与 `AbTestConfig` 一致的标签）
2. 菜单 **vFramework → Build Test AB**
3. 打开 **ResTest** 场景，按下方绑定 UI

---

## 问题1：AB 有没有「大包套小包」？

**没有。** 一个 `.ab` 文件 = 一个 AssetBundle，**不会再嵌套多个 AB 文件**。

但 **一个 AB 文件里可以装很多 Unity 资源**（多张贴图、多个 prefab），像一个大箱子装多件物品。

| 误解 | 实际 |
|------|------|
| `demo/icon` 里还有 `1.ab`、`2.ab` | `demo/icon` **就是**一个文件，里面有 `1.png`、`2.png` 等资源 |
| 加载某个 icon 要「打开子包」 | `LoadFromFile("demo/icon")` 一次，再 `LoadAsset<Texture>("3")` |

**演示代码**：`AbDemoRunner.Demo1_LoadOneIconFromMultiAssetBundle`

| API | 作用 |
|-----|------|
| `AssetBundle.LoadFromFile(path)` | 把整包加载进内存 |
| `bundle.LoadAsset<Texture>("3")` | 从该包内按**资源名**取其中一份 |

**建议绑定 UI**：

| 组件 | 绑定 |
|------|------|
| Button | `AbDemoRunner.Demo1_LoadOneIconFromMultiAssetBundle` |
| RawImage（可选） | 拖到 `AbDemoRunner.iconPreview`，用于显示加载的 3.png |

---

## 问题2：同名资源怎么处理？

### 2a. 两个同名 Prefab（你的例子）

| 资源 | AB 包名 |
|------|---------|
| `UI/TestUI.prefab` | `demo/ui/testui` |
| `UI/Test/TestUI.prefab` | `demo/ui/testui_alt` |

包内 **Name 都是 `TestUI`**，但 **AB 包名不同** → 先 `LoadFromFile(包名)` 再 `LoadAsset("TestUI")`，不会混。

**演示**：

- `Demo2_LoadRootTestUI` → 加载 `Assets/AssetBundle/UI/TestUI.prefab`
- `Demo2_LoadAltTestUI` → 加载 `UI/Test/TestUI.prefab`（带 `TestUI.cs`，可换背景/模型）

### 2b. 同名不同 Unity 类型

例如包 `demo/model/ji_mat` 里只有 `lambert2.mat`：

```csharp
bundle.LoadAsset<Material>("lambert2");   // 有值
bundle.LoadAsset<GameObject>("lambert2"); // null
```

**API 的泛型 `LoadAsset<T>` 按类型区分**，不是按字符串再分。

**演示**：`Demo2_SameNameDifferentType`

**建议绑定 UI**：

| Button | 方法 |
|--------|------|
| Q2-Root TestUI | `Demo2_LoadRootTestUI` |
| Q2-Alt TestUI | `Demo2_LoadAltTestUI` |
| Q2-同名不同类型 | `Demo2_SameNameDifferentType` |
| Transform（可选） | `AbDemoRunner.spawnRoot` |

---

## 问题3：A 包依赖 B 包的资源怎么办？

打包时 Unity 会记录 **AB 之间的依赖**（例如 `Ji.prefab` 依赖 `lambert2.mat` 所在包）。

运行时：

1. 查 `AssetBundleManifest.GetAllDependencies("demo/model/ji")`
2. **先** `LoadFromFile` 依赖包，**再** Load 主包
3. 否则实例化 Ji 可能 **缺材质/网格**（粉红、Missing）

**演示**：

- `Demo3_LoadJiWithDependencies` — 正确：经 `AbManifestLoader` 自动先 Load 依赖
- `Demo3_LoadJiWithoutDependencies` — 对比：故意跳过依赖，观察异常

| API | 作用 |
|-----|------|
| `AssetBundle.LoadFromFile(.../AssetBundles)` | 加载平台总 manifest 包 |
| `LoadAsset<AssetBundleManifest>("AssetBundleManifest")` | 得到依赖表 |
| `manifest.GetAllDependencies(bundleName)` | 查某 AB 依赖哪些 AB |

**建议绑定 UI**：

| Button | 方法 |
|--------|------|
| Q3-带依赖加载 Ji | `Demo3_LoadJiWithDependencies` |
| Q3-跳过依赖（对比） | `Demo3_LoadJiWithoutDependencies` |
| 卸载全部 | `Demo_UnloadAll` |

---

## 附加：加载 Prefab 后替换子资源

1. 先 **Demo2_LoadAltTestUI**（实例上有 `TestUI.cs` 和按钮）
2. 在实例 UI 上点「换背景」等 — 走 `TestUI.OnChangeBackground`（从 `demo/background` 等包 Load 贴图赋给 RawImage）

或绑定 **DemoExtra_ReplaceBackgroundOnSpawnedTestUI** 触发一次换背景。

---

## 场景结构建议（不生成 UI，自行搭建）

在 **ResTest** 场景：

```
Canvas
├── Btn_Q1_Icon          → Demo1_LoadOneIconFromMultiAssetBundle
├── Btn_Q2_RootTestUI    → Demo2_LoadRootTestUI
├── Btn_Q2_AltTestUI     → Demo2_LoadAltTestUI
├── Btn_Q2_SameNameType  → Demo2_SameNameDifferentType
├── Btn_Q3_WithDeps      → Demo3_LoadJiWithDependencies
├── Btn_Q3_NoDeps        → Demo3_LoadJiWithoutDependencies
├── Btn_UnloadAll        → Demo_UnloadAll
├── RawImage_Preview     → 绑到 AbDemoRunner.iconPreview
└── (可选) ABTestRoot
     ├── ResTest          → 旧版单按钮加载 demo/ui/testui
     └── AbDemoRunner     → 上述演示入口
SpawnRoot (Empty)        → AbDemoRunner.spawnRoot
```

---

## 代码文件

| 文件 | 说明 |
|------|------|
| `AbTestConfig.cs` | 包名常量 |
| `AbManifestLoader.cs` | 依赖加载 + 包缓存 |
| `AbDemoRunner.cs` | 三问演示 + 详细注释 |
| `AbDemoLabelApplier.cs` | Editor 一键打标签 |
| `TestUI.cs` | Prefab 内换图/模型（依赖 AB） |

---

## 分包一览（Apply Demo AB Labels 后）

| 路径 | AB 名 |
|------|-------|
| `UI/TestUI.prefab` | `demo/ui/testui` |
| `UI/Test/TestUI.prefab` | `demo/ui/testui_alt` |
| `Icon/` | `demo/icon` |
| `Background/` | `demo/background` |
| `Atlas/` | `demo/atlas` |
| `Model/Ji.prefab` | `demo/model/ji` |
| `Model/ji/cai/lambert2.mat` | `demo/model/ji_mat` |
