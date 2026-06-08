# Catalogue 清单说明

> 打包器与加载器之间的桥梁。当前实现：**JSON + 仅 `entries`**；本文说明现有字段、规划中的 **bundle 依赖表**，以及 **如何接入**。

相关文件：

| 文件 | 作用 |
|------|------|
| `BundleRuleConfig/Catalogue/AssetCatalog.cs` | 清单数据结构（`AssetCatalogEntry` / `BundleCatalogInfo` / `AssetCatalog`，#region 分区） |
| `Editor/BundleBuilder/CatalogueWriter.cs` | 打包后写清单 |
| `ResLoader/BundleManager.cs` | 运行时 LoadFromFile（尚未按依赖预加载） |

---

## 一、当前清单：`entries`

每条记录回答：**这个资源在哪个 bundle 里、叫什么名字**。

```json
{
  "assetPath": "Assets/AssetBundle/UI/UIRoot.prefab",
  "bundleName": "ui.bundle",
  "assetName": "UIRoot"
}
```

加载流程（目标态）：

1. `LoadByPath(assetPath)` 在 `entries` 里查找；
2. 得到 `bundleName`、`assetName`；
3. `BundleResLoader.Load<T>(bundleName, assetName)`。

---

## 二、为什么要单独的依赖表？

Unity 打 AB 时，包与包之间已有依赖。例如 `ui.bundle.manifest`：

```yaml
Dependencies:
- .../atlas.bundle
- .../background.bundle
- .../common.bundle
- .../icon.bundle
```

若只加载 `ui.bundle` 再 `LoadAsset`，跨包引用的材质/图集可能失败或显示异常，需要 **先加载依赖包**。

### 不应写在 `AssetCatalogEntry` 上

- 同一 bundle 内上百条 asset，**依赖完全相同**，逐条重复会撑大清单（JSON/未来二进制都不划算）。
- 依赖是 **bundle 与 bundle** 的关系，不是 asset 级别。

### 推荐结构：`bundles[]`

在 `AssetCatalog` 上增加（规划，尚未启用）：

```json
{
  "version": "1.0.0",
  "entries": [ ... ],
  "bundles": [
    {
      "bundleName": "ui.bundle",
      "dependencies": [
        "atlas.bundle",
        "background.bundle",
        "common.bundle",
        "icon.bundle"
      ]
    },
    {
      "bundleName": "atlas.bundle",
      "dependencies": []
    }
  ]
}
```

约定：

- 只存 **bundle 文件名**（如 `atlas.bundle`），不存 `F:/...` 绝对路径；
- 只存 **直接依赖**；加载时递归展开即可；
- 无依赖的包：`dependencies: []` 或省略该条（实现时二选一，建议显式空数组）。

对应 C# 类型：`BundleCatalogInfo`（`bundleName` + `dependencies[]`）。

---

## 三、依赖数据从哪来？

**权威来源**：Unity 本次 `BuildPipeline.BuildAssetBundles` 生成的 **AssetBundleManifest**（不是手填）。

打包后输出目录里会有：

- 各 `*.bundle` + `*.bundle.manifest`（单包 YAML，含 Dependencies）；
- 平台总 manifest 文件（如 `StreamingAssets` / `StandaloneWindows64` 等），可用 API 查询。

### 推荐写法（Editor，`CatalogueWriter` 内）

在 **已执行 `BuildPipeline.BuildAssetBundles` 之后**（`EditorTest` 模式无 manifest 时可跳过或留空）：

```csharp
// 1. 返回值即 Manifest 包装 bundle
AssetBundle manifestBundle = BuildPipeline.BuildAssetBundles(
    bundleRoot, builds, options, target);

if (manifestBundle == null)
    return null; // EditorTest 等未打 AB 的情况

// 2. 取出 Manifest 对象
AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

// 3. 对本批每个 bundle 查直接依赖
foreach (AssetBundleBuild build in builds)
{
    string bundleName = build.assetBundleName;
    string[] deps = manifest.GetAllDependencies(bundleName);
    // deps 可能含自身，需 Filter；路径可能是相对 bundleRoot 的路径

    List<string> depNames = new List<string>();
    foreach (string dep in deps)
    {
        if (dep == bundleName)
            continue;
        depNames.Add(Path.GetFileName(dep)); // 规范为 atlas.bundle
    }

    bundles.Add(new BundleCatalogInfo
    {
        bundleName = bundleName,
        dependencies = depNames.ToArray()
    });
}

manifestBundle.Unload(true);
```

注意：

- `GetAllDependencies` 返回的是 **含递归** 的全量依赖；若只要直接依赖，需对照文档或解析单包 `.manifest` 的 `Dependencies` 段。
- 若用全量依赖，加载时按列表顺序依次 `AcquireBundle` 即可，不必再递归。
- `BuildPipeline.BuildAssetBundles` 当前在 `BundleBuilder.BuildByMode` 调用，**Manifest 应在同一 `bundleRoot` 下读取**，再传给 `CatalogueWriter.Write(..., manifest)`。

---

## 四、接入步骤 checklist（实施时用）

1. **数据结构**  
   - 取消 `AssetCatalog.cs` 里 `bundles` 字段的注释。  
   - 类型均在 `AssetCatalog.cs`（`BundleCatalogInfo` 等）。

2. **CatalogueWriter**  
   - `BuildCatalog` 增加 `BundleCatalogInfo[] bundles` 参数或内部 `BuildBundleDependencies(manifest, builds)`。  
   - `Write` 在 `BundleBuilder` 侧传入 Manifest（或 `bundleRoot` + 平台 manifest 路径）。  
   - `EditorTest`：无 AB 时 `bundles` 写空数组或根据 Editor 依赖分析补全（可选，P2）。

3. **BundleBuilder**  
   - 保存 `BuildAssetBundles` 返回值，传给 `CatalogueWriter`（仅非 EditorTest）。

4. **CatalogueReader**（未建）  
   - 读 JSON/未来二进制；提供 `GetBundleDependencies(bundleName)`。

5. **BundleManager**  
   - `AcquireBundleWithDependencies(bundleName)`：查清单 → 先加载 dependencies → 再加载本体。  
   - 引用计数：每个被 Acquire 的包 Ref+1；Release 时勿过早卸载仍被其它资源引用的依赖包。

6. **验收**  
   - 打 ui.bundle 后，清单中 `bundles` 与 `ui.bundle.manifest` 的 Dependencies 一致（仅文件名形式）。  
   - 运行时只 Load ui 包内 prefab，不手动 Load atlas，仍能正常显示。

---

## 五、与 Unity `.manifest` 的关系

| 来源 | 用途 |
|------|------|
| `{name}.bundle.manifest` | 单包 YAML，调试、对比 |
| `AssetBundleManifest` API | **打包时写入 Catalogue 的首选** |
| `AssetCatalog.bundles` | **运行时加载器单入口**，路径统一、可二进制化 |

不必在运行时解析 YAML；Catalogue 是项目自己的「加载用 manifest」。

---

## 六、其它规划（见 TODO）

- 清单格式 **JSON → 二进制**（性能/加密）：`bundles` 适合作为独立索引段，与 `entries` 并列。  
- 详细待办可集中写在 `Docs/TODO.md`（若已创建）。
