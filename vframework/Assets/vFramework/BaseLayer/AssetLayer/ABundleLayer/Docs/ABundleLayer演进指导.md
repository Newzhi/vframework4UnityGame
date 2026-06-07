# ABundleLayer 演进指导

> 本文说明 ABundleLayer 当前能力边界与后续演进方向。  
> 架构细节见 [ABundleLayer架构说明.md](./ABundleLayer架构说明.md)，对标分析见 [方案对比与学习指南.md](../../方案对比与学习指南.md)。

---

## 1. 当前阶段（阶段 2～3）

### 已完成

| 能力 | 说明 |
|------|------|
| 四层架构 | 规则制定器 → 打包器 → 抽象资源层 → 加载器 |
| 平台分目录输出 | `{OutputPath}/{Windows\|iOS\|Android}/` |
| Catalog 寻址 | 业务只认 `location` |
| 引用计数 + LoadTicket | 依赖链 Retain/Release，避免依赖包泄漏 |
| **IAssetHandle** | `LoadHandle<T>` 返回句柄，`Release()` 释放 |
| **ABundleScopeLoader** | 单元级加载，`OnDestroy` 自动 `RecycleAll` |
| EditorSimulation | Editor 下 AssetDatabase 直读 |
| Analyzer | Manifest 依赖 / 反向依赖 / Location 查询 |
| 运行时测试 | `Assets/Test/ABundleTest/` 全场景 + 内存 log |

### 刻意暂缓（保持精简）

- **ResMgr 全局门面** — 接口已占位，暂不实现
- **真异步** — `LoadAssetAsync` / `LoadHandleAsync` 仅为同步+回调占位
- 多 Package、热更下载、加密、二进制 Manifest

---

## 2. 推荐 API 用法

```csharp
// 推荐：单元 Scope + Handle
var scope = gameObject.AddComponent<ABundleScopeLoader>();
var handle = scope.LoadHandle<Texture2D>("icon/3");
var tex = handle.GetAsset<Texture2D>();
// OnDestroy 时 scope 自动 RecycleAll

// 或手动句柄
var loader = new ABundleLoader();
loader.InitializeFromRules(rules);
var h = loader.LoadHandle<GameObject>("ui/test/testui");
h.Release();
loader.Shutdown();

// 兼容旧 API（单 location 追踪）
var asset = loader.LoadAsset<Texture2D>("icon/3");
loader.ReleaseAsset("icon/3");
```

**原则：** 每次 `LoadHandle` 必须配对 `Release`；UI/场景脚本优先用 `ABundleScopeLoader`。

---

## 3. 近期可做（阶段 3 收尾）

按优先级排序，每项独立可交付：

| 序号 | 项 | 价值 |
|------|-----|------|
| 1 | **延迟卸载队列** | RefCount 归零后不立刻 Unload，下一帧或 N 秒后再卸，减少抖动 |
| 2 | **Editor / Runtime 测试矩阵** | CI 或菜单一键跑 EditorSimulation + RuntimeBundle 双模式 |
| 3 | **Analyzer ↔ BuildReport 联动** | 分析器直接打开最近一次 `ABundleBuildReport.json` |
| 4 | **ResTest 迁移** | 旧 `AbManifestLoader` 演示改为 ABundleLoader（可选） |
| 5 | **Instantiate 封装** | Handle 扩展 `Instantiate()` 并在 Release 时 Destroy 实例（可选） |

---

## 4. 中期（阶段 4 入口）

与 YooAsset / ResKit 对齐时的自然延伸：

| 能力 | 参考 | 说明 |
|------|------|------|
| ResMgr 门面 | ResKit ResourceManager | 统一 Init、Load、全局缓存策略 |
| 真异步 | YooAsset AssetHandle | `LoadAssetAsync` 走 Unity 异步 API + 完成回调/await |
| ResourcePackage | YooAsset | 多包/多版本隔离 |
| 热更下载器 | YooAsset Downloader | 远端清单 + 差量下载 + 校验 |

**建议顺序：** Handle/Scope（已完成）→ 延迟卸载 → ResMgr → 真异步 → 热更。

---

## 5. 能力对照（精简版）

| 能力 | ABundleLayer | ResKit | YooAsset |
|------|:------------:|:------:|:--------:|
| 规则/XML 分包 | ✓ | 部分 | ✓ |
| Catalog location | ✓ | 路径/key | ✓ |
| 引用计数 | ✓ | ✓ | ✓ |
| Handle + Release | ✓ | Res | AssetHandle |
| 单元 Scope 释放 | ✓ | ResLoader | Package 内管理 |
| Editor 模拟模式 | ✓ | ✓ | ✓ |
| 全局 ResMgr | 占位 | ✓ | Package |
| 真异步 | 占位 | ✓ | ✓ |
| 热更下载 | — | 扩展 | ✓ |
| 多 Package | — | — | ✓ |

---

## 6. 测试与质量

- **Editor：** `vFramework/AssetKit/ABundleAnalyzer`（UI 占位，分析逻辑待实现）
- **Runtime：** `Assets/Test/ABundleTest/ABundleLoadTestRunner` 全场景 + 内存 log
- **泄漏判定：** 对比压力测试前后 Mono/Allocated；RefCount 不为 0 或包未卸载即 `LEAK_SUSPECT`

打包后务必在目标平台目录验证 Catalog 与 Manifest 同时存在。

---

## 7. 目录演进预期

```
ABundleLayer/
├── Shared/                # 规则/Catalog 数据模型
├── Layer3_Resource/       # 包缓存、Ticket、Catalog、包级引用计数
├── Layer4_Loader/         # Loader、Handle、ScopeLoader
├── Editor/                # Layer1_RuleEditor、Layer2_Packer、Analyzer
├── Demo/
└── Docs/
    ├── ABundleLayer架构说明.md
    └── ABundleLayer演进指导.md   ← 本文
```

新增能力优先放入已有层，避免平行 Manager 类泛滥；ResMgr 将来作为 **Loader 之上的薄门面**，不替代 ResourceSystem。

---

*与代码同步维护。完成阶段 3 收尾后更新「已完成」表格。*
