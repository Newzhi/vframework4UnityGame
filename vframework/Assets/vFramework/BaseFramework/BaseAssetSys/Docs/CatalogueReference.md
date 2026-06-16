# Catalogue 清单说明

> 打包器与加载器之间的桥梁。当前实现：**JSON + `entries` + `bundles[]`**；运行时由 `CatalogueReader` 只读。  
> 文档索引：[Docs/DocumentIndex.md](./DocumentIndex.md)  
> **拓扑排序与构建优化计划**：[BundleBuildOptimizationAndTopologyPlan.md](./BundleBuildOptimizationAndTopologyPlan.md)

相关文件：

| 文件 | 作用 |
|------|------|
| `BundleRuleConfig/Catalogue/AssetCatalog.cs` | 清单数据结构（`AssetCatalogEntry` / `BundleCatalogInfo` / `AssetCatalog`，#region 分区） |
| `Editor/BundleBuilder/CatalogueWriter.cs` | 打包后写清单（含 Manifest 依赖） |
| `ResLoader/Catalogue/CatalogueReader.cs` | 运行时读 JSON |
| `ResLoader/Bundle/BundleManager.cs` | 按 `bundles[]` 依赖预加载后 LoadFromFile |

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

加载流程：

1. 业务调用同步 `Load("Atlas/Role/Hog_Attack_000")`（相对 `resourceRoot` 简路径）；
2. `CatalogueReader.TryGetEntryByLoadPath` 查表（由 `entries` + `resourceRoot` 构建）；
3. 得到 `bundleName`、`assetName` → `LoadByBundle` → 依赖预加载 → `LoadAsset`。

辅助：`LoadByAssetPath` 用 Unity 完整 `assetPath` 查 `entries`；`LoadByBundle` 直接按包名桥接。

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

### 推荐结构：`bundles[]`（✅ 已启用）

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
- 当前写入 **全量依赖**（`GetAllDependencies`），并经 **`BundleDependencyTopology` 拓扑排序**（叶→根，供 `AcquireBundleWithDependencies` 顺序 Acquire）；
- 无依赖的包：`dependencies: []` 或省略该条（实现时二选一，建议显式空数组）。

对应 C# 类型：`BundleCatalogInfo`（`bundleName` + `dependencies[]`）。

### P1-B 扩展字段（✅ 已实现）

| 字段 | 位置 | 说明 |
|------|------|------|
| `buildId` | `AssetCatalog` 根 | 本次构建 GUID，关联 `Reports/BuildManifest.json` |
| `catalogueHash` | 根 | 整份清单 SHA256（不含本字段），运行时 CDN 比对 |
| `cdnBaseUrl` | 根 | CDN 根 URL（末尾无斜杠）；打包时从 `BuildSetting.cdnBaseUrl` 写入；Init 注入 RemoteProvider |
| `compressionMode` | 根 | `LZMA` / `LZ4Chunk` / `Uncompressed` |
| `resourcePriority` | `bundles[]` | 对应 `ResourcePriority` 整型；越小越不易 LRU 卸载（运行时 `BundleLruUnloadPolicy`） |
| `sizeBytes` / `fileHash` / `crc32` | `bundles[]` | 构建后 .bundle 完整性 |
| `dependenciesAll` | `bundles[]` | 可选；`useDirectDependenciesOnly=true` 时存全量传递依赖 |

Editor 增量产物（同 `bundleRoot/Reports/`）：

- `BuildManifest.json` — 各包 hash/crc/优先级快照  
- `BuildManifest.diff.json` — 相对上一份的 added/removed/changed  
- `BuildCache.json` — 源 GUID hash + 输出 hash，供「增量打包」跳过 Unity 构建  
- `DependencyGraph.json` — bundle 依赖图（直接/反向/传递闭包），Reporter **依赖 Explorer** 读取

### C-3 运行时路径（阶段 C ✅）

| 层级 | 路径 | 说明 |
|------|------|------|
| 热更缓存 | `persistentDataPath/ABCache/{平台}/` | CDN 下载的 bundle + 热更清单 `Catalogue/AssetCatalog.json` |
| 首包 | `StreamingAssets/{平台}/` | 安装包内置 subset |
| 远程 | 清单 `cdnBaseUrl` + `/{bundleName}` | `HttpRemoteBundleProvider` HTTP 拉取 |

解析顺序：**ABCache → StreamingAssets → CDN**（`DefaultBundlePathResolver` + `AssetRouter` NETCDN）。

### 拓扑序约定（✅ 已实现）

- **边语义**：若 `ui.bundle` 依赖 `atlas.bundle`，则 `dependencies[]` 中 **`atlas.bundle` 排在 `ui.bundle` 之前**（叶→根）。
- **写端**：`CatalogueWriter.TryBuildBundleDependencies` 用 Manifest 直接依赖建图 + Kahn 排序；**环检测失败**或排序改变集合 → `Write` 返回 `false`，不写 JSON。
- **读端**：`CatalogueReader.BuildLookupTables` 对每条 `dependencies` 用 `SortUsingCatalogAllDeps` **幂等再排序**（双保险）。
- **开关**：`BuildSetting.useTopologicalSort`（默认 `true`）；`false` 时回退 Unity 原始顺序。

### `Write` 失败条件

| 条件 | 行为 |
|------|------|
| 依赖环 | `LogError`，不写清单 |
| 拓扑排序改变依赖集合 | `LogError`，不写清单 |
| loadPath 重复 + `loadPathDuplicateAsError=true` | `LogError`，不写清单 |
| loadPath 重复 + 默认 `false` | `LogWarning`，仍写清单 |

loadPath 校验由 `CatalogueValidator.ValidateEntries` 执行，Analyzer 报告复用同一结果。

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

## 四、接入步骤 checklist

1. **数据结构** — ✅ 已启用 `AssetCatalog.bundles`  
2. **CatalogueWriter** — ✅ `BuildBundleDependencies` + `Write(..., manifest)`  
3. **BundleBuilder** — ✅ 捕获 `BuildAssetBundles` 返回值  
4. **CatalogueReader** — ✅ `ResLoader/Catalogue/CatalogueReader.cs`  
5. **BundleManager** — ✅ `AcquireBundleWithDependencies`  
6. **验收** — 手动：DeviceDebug 打包 + `ABLoadSmokeTest`（L-024 / L-033 / P-055）

### C-3 运行时路径（阶段 C ✅）

| 位置 | 角色 |
|------|------|
| `{persistentDataPath}/ABCache/{平台}/` | CDN 已下载 bundle + 热更清单 |
| `StreamingAssets/{平台}/` | 首包内置 bundle + 首包清单 |
| 清单 `cdnBaseUrl` | 远程根 URL；Init 注入 `HttpRemoteBundleProvider` |

解析顺序：**ABCache → 首包 → CDN 下载**（见 [DesignGoalsAndImplementation.md](./DesignGoalsAndImplementation.md)「首包、热更包与本地缓存」）。

---

## 五、与 Unity `.manifest` 的关系

| 来源 | 用途 |
|------|------|
| `{name}.bundle.manifest` | 单包 YAML，调试、对比 |
| `AssetBundleManifest` API | **打包时写入 Catalogue 的首选** |
| `AssetCatalog.bundles` | **运行时加载器单入口**，路径统一、可二进制化 |

不必在运行时解析 YAML；Catalogue 是项目自己的「加载用 manifest」。

---

## 六、其它规划

- 清单 **JSON → 二进制**（性能/加密）：见 [MainRoadmap.md](./MainRoadmap.md) P3。  
- 运行时 **version/buildNumber 比对**：阶段 C，见 [BusinessApiAndCdnPlanning.md](./BusinessApiAndCdnPlanning.md) §2。
