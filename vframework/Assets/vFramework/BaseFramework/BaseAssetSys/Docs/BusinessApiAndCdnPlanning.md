# 业务 API 与 CDN 规划

> 对照 [MainRoadmap.md](./MainRoadmap.md) 阶段 B/C；实现细节见 [ResLoader/LoaderDesignGuide.md](../ResLoader/LoaderDesignGuide.md)。

---

## 一、业务侧需求对照表

| # | 需求（设计基线） | 目标 API（形态） | 现状 | 计划模块 |
|---|------------------|------------------|------|----------|
| 1 | 同步加载资源 | `Load<T>(loadPath)` 简路径；`LoadByBundle` / `LoadByAssetPath` 辅助 | ✅ 已实现 | `BundleResLoader` |
| 2 | 异步加载（设计基线 **默认 API**） | `LoadUniTaskAsync<T>(loadPath)` / `await` 形态 | ✅ 已接入（基础版） | `BundleResLoader` + UniTask |
| 3 | 加载 + 完成回调 | `LoadUniTaskWithCallback<T>(path, onComplete, onFailed, useUniTask)` | ✅ 已实现 | 基于 `Load/LoadUniTaskAsync` 封装 |
| 4 | 预加载资源包 | `PreLoadBundle(moduleName)` / 按 bundle 列表预热 | ❌ 占位 | `BundleManager` 或 `BundleResLoader` |
| 5 | 卸载单个资源 | `IAssetHandle.Release()` 或 `Unload(handle, instance, cb)` | ✅ Resource 立即卸；Bundle **LRU 延迟**（§5.6） | `BundleResLoader` + `BundleManager` |
| 6 | 卸载全部 | `BundleResLoader.UnloadAll()` | ✅ 立即全卸 Resource + Bundle | `BundleResLoader` + `BundleManager` |
| 7 | **CDN 联网下载** | 远程清单对比 → 下载 AB → 本地缓存 → 再 Load | 🟡 **首版** `HttpRemoteBundleProvider` | 见下文 §二 |

**测试要求**（设计基线）：依赖顺序 ✅；异常 Log 🟡；**竞态安全 ✅**（同步双 Runner 三端 19/19）；引用计数 ✅。

> 异步说明：当前 `LoadUniTaskAsync` 已提供 UniTask `await` 入口，内部仍复用同步 `Load` 完成 AB 读取；真实下载队列/并发合并/后台 I/O 仍在后续迭代。

> UniTask 依赖：通过 `Packages/manifest.json` 的 OpenUPM 源接入 `com.cysharp.unitask: 2.5.10`。

---

## 二、CDN 联网加载（扩展点，首版已接）

### 2.1 与打包模式的关系

| 打包模式 | 编辑器产出 | 运行时角色 |
|----------|------------|------------|
| **DeviceDebug / 首包** | `deviceOutputPath`（默认 StreamingAssets） | 安装包内置，离线可用 |
| **CdnHotUpdate / CDN联网** | `cdnOutputPath`（默认 `Bundles/CDN`） | CI 上传到 **`CDN/{平台}/`**，如 `CDN/Android/`、`CDN/StandaloneWindows64/` |
| **DlcPackage** | 规划独立目录 | 按需下载（TODO） |

打包器只负责 **打出文件 + 写清单**；**下载与运行时选路** 属于加载侧扩展。

### 2.2 运行时资源查找优先级（目标态）

```text
persistentDataPath / ABCache /     ← CDN 已下载的新版 bundle + 清单
    ↓ 未命中
StreamingAssets / 首包 bundleRoot  ← 安装包内置
    ↓ 未命中
CDN / OSS                          ← HTTP(S) 下载，写入 persistentDataPath 后重试 Load
```

与清单字段：`catalogueHash` / `buildId` 用于对比 **是否需拉新清单**；`bundles[].fileHash` / `crc32` 用于 **下载校验**；`resourcePriority` 用于运行时 LRU 卸包与（规划中的）CDN 下载队列排序。

### 2.3 建议模块划分（代码扩展点）

```text
BundleResLoader          业务 API 不变
    ↓
AbstractResource         LoadAsset / Release
    ↓
AssetRouter              RouteAssetSource → 四 Provider
    ↓
BundleManager            AcquireBundle 前解析物理路径
    ↓
IBundlePathResolver      本地多根目录优先级（首包 / 缓存）     ← ✅ DefaultBundlePathResolver
    ↓
IRemoteBundleProvider    清单 hash 比对、HTTP 下载、CRC 校验       ← 🟡 HttpRemoteBundleProvider（首版）
```

**当前代码**：`AbstractResource` 经 `AssetRouter` 加载；`BundleManager.AcquireBundle` 优先 `IBundlePathResolver`；本地无包时路由 `NETCDN` → `HttpRemoteBundleProvider`（`catalogueHash` 比对 + `UnityWebRequest` 下载单包）。  
业务 **不应** 直接写 UnityWebRequest；须注入 `AssetRouter.Init(..., remoteProvider)` 并配置 `BuildSetting.cdnBaseUrl`。

### 2.4 CDN 接入步骤（实施 checklist）

1. **配置**：`BuildSetting.cdnBaseUrl` + **`{Platform}/`** 子路径（与 `usePlatformSubfolders` 产出一致）。  
2. **启动**：`HttpRemoteBundleProvider` 拉远程 `AssetCatalog.json`，与本地比 `catalogueHash`（🟡 首版）。  
3. **下载**：按 bundle 名 HTTP 拉取 `.bundle`，`fileHash` / `crc32` 校验后写入 `persistentDataPath/...`（🟡 首版）。  
4. **Init**：`BundleResLoader.Init(cacheRoot)` 或 `IBundlePathResolver` 多 root。  
5. **Load**：同步 `Load(loadPath)` 与依赖预加载不变；业务侧异步入口统一为 **LoadUniTaskAsync**（UniTask）。

### 2.5 本阶段明确不做

- 断点续传、后台下载队列、按 `resourcePriority` 并发调度  
- 多 CDN 容灾、加密 bundle  
- 拉新清单后 **自动替换** 本地 `CatalogueReader` 缓存（首版仅比对 Log）

**已实现（2026-06-08）**：`AssetRouter` 四源统一入口；EditorTest 走 AssetDatabase；`Resources/` 前缀走 Resources。  
**已实现（2026-06-13）**：`HttpRemoteBundleProvider` 首版；清单 `catalogueHash` / 单包 hash·CRC 打包写入；Bundle **LRU 延迟卸包**（业务 API 不变，见 [BusinessApiUsageGuide.md §5.6](./BusinessApiUsageGuide.md#56-bundle-lru-延迟卸载业务无感)）。  
**仍留后续**：下载队列、本地 Catalogue 热更、完整热更决策流。

---

## 三、与禁止区设计目标的对应

| 设计目标场景 | 依赖能力 |
|--------------|----------|
| 边玩边下、300MB 以下首包 | CDN 下载 + 清单版本 + 按需 Load |
| 玩家自选关卡/DLC | DLC 分包模式 + `IRemoteBundleProvider` |
| 单机可玩老版本 | 本地缓存清单世代 ≤ 远程失败时回退首包 |
| MOD 上传下载 | 独立 Package / CDN 路径（远期） |

---

## 四、相关文档

- [MainRoadmap.md](./MainRoadmap.md) — 阶段 B/C 排期  
- [DesignGoalsAndImplementation.md](./DesignGoalsAndImplementation.md) — 首包 / CDN / persistentDataPath 目录约定  
- [CatalogueReference.md](./CatalogueReference.md) — 清单字段与版本号  
- [ResLoader/LoaderDesignGuide.md](../ResLoader/LoaderDesignGuide.md) — 双层加载与 API 现状  
