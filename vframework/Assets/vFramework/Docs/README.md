# vFramework 文档索引

> 路径：`Assets/vFramework/Docs/`  
> 资源加载子系统另有独立索引：[BaseAssetSys/Docs/DocumentIndex.md](../BaseFramework/BaseAssetSys/Docs/DocumentIndex.md)

本目录存放 **框架级** 文档（分层、接入、通用参考）。模块实现细节见各子目录 `README.md`（如 `BaseGameRoot/`、`SceneLayer/`）。

---

## 分类目录

| 子目录 | 用途 | 文档 |
|--------|------|------|
| **[Overview/](./Overview/)** | 框架定位、三层架构、热更边界 | [ProjectGoals.md](./Overview/ProjectGoals.md)、[FrameworkDesign.md](./Overview/FrameworkDesign.md) |
| **[Guides/](./Guides/)** | 业务如何接入、迁移旧代码 | [StandaloneAndResourceHotfixGuide.md](./Guides/StandaloneAndResourceHotfixGuide.md) |
| **[Reference/](./Reference/)** | 与框架解耦的通用参考 | [AssetBundleGuide.md](./Reference/AssetBundleGuide.md) |

---

## 推荐阅读顺序

### 新项目 / 单机 / 只热更资源

1. [ProjectGoals.md](./Overview/ProjectGoals.md) — 框架做什么、不做什么  
2. **[StandaloneAndResourceHotfixGuide.md](./Guides/StandaloneAndResourceHotfixGuide.md)** — 启动、注册业务、Mono 迁移（**首选实操**）  
3. [BaseGameRoot/README.md](../BaseFramework/BaseGameRoot/README.md) — GameRoot / TryStart / Module  
4. [GameLaunch/README.md](../BaseFramework/BaseGameRoot/GameLaunch/README.md) — AOT 启动与 `autoLaunchOnAwake`  
5. [BusinessApiUsageGuide.md](../BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md) — 资源 Load / Release / CDN  

### 需要代码热更（HybridCLR）

在上一路径基础上增加：

- [FrameworkDesign.md §8](./Overview/FrameworkDesign.md) — AOT / 热更程序集边界  
- [GameLaunch/README.md §2.2](../BaseFramework/BaseGameRoot/GameLaunch/README.md) — `HotfixReflection` 路径  

### 架构与排期

| 文档 | 范围 |
|------|------|
| [FrameworkDesign.md](./Overview/FrameworkDesign.md) | 三层架构、MVC + Proxy、数据流 |
| [BaseAssetSys/Docs/MainRoadmap.md](../BaseFramework/BaseAssetSys/Docs/MainRoadmap.md) | 资源打包/加载 **唯一排期** |
| [BaseFramework/README.md](../BaseFramework/README.md) | AOT 固定层约定 |

---

## 模块文档（不在 Docs/ 内）

| 模块 | 文档 |
|------|------|
| GameRoot / IOC | [BaseGameRoot/README.md](../BaseFramework/BaseGameRoot/README.md) |
| 宏观流程 | [GameFlow/GameFlowApi.md](../BaseFramework/BaseGameRoot/GameFlow/GameFlowApi.md) |
| 场景调度 | [SceneLayer/README.md](../BaseLayer/SceneLayer/README.md) |
| 配置表 | [ConfigTableLayer/配表工具生成契约.md](../BaseLayer/ConfigTableLayer/配表工具生成契约.md) |
| 事件总线 | [BaseEventSys/README.md](../BaseFramework/BaseEventSys/README.md) |

---

## 文档维护约定

- **排期 / TODO** → 只写 [MainRoadmap.md](../BaseFramework/BaseAssetSys/Docs/MainRoadmap.md)，不在本目录重复。  
- **业务 API 范例** → [BusinessApiUsageGuide.md](../BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md)。  
- **模块职责** → 对应代码目录 `README.md`。  
- 新建框架级指南 → 放入 `Guides/`，并更新 **本文** 分类表。
