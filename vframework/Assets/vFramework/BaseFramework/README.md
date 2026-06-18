# BaseFramework（AOT 固定层）

> 路径：`Assets/vFramework/BaseFramework/`  
> **本目录下全部代码属于 AOT 固定框架层**：随安装包编译进主程序集，**不**通过 HybridCLR 热更替换。

上层文档：[FrameworkDesign.md](../Docs/FrameworkDesign.md) §8、[ProjectGoals.md](../Docs/ProjectGoals.md)

---

## 1. 约定（强制）

| 项 | 规定 |
|----|------|
| **目录** | `Assets/vFramework/BaseFramework/**` 全部为 AOT |
| **变更方式** | 仅随 **App 发版** 更新；不走热更 DLL |
| **职责** | 与具体玩法无关的基础设施：启动管道、IOC、资源系统内核、事件、网络编解码、日志等 |
| **禁止** | 在本目录新增 **业务玩法**、**项目专属 Bootstrap 装配**、**配表生成类**（应放在热更目录） |

```text
AOT（固定）                    热更（HybridCLR，目录见 §3）
BaseFramework/                 HotUpdateScripts/、HotUpdateLayer/ 等
  BaseGameRoot/                  GameBootstrap、AppEntry
  BaseAssetSys/                  玩法 Module / Service
  BaseEventSys/                  Proxy / Controller / Model
  …                              MetaConfigs 生成代码
```

---

## 2. 本目录包含什么

| 子目录 | 说明 |
|--------|------|
| `BaseGameRoot/` | `GameRoot`、IOC、`ModuleManager`、`IGameBootstrap` 接口、GameTime / GameFlow 框架 Module、GameLaunch |
| `BaseAssetSys/` | AB 打包与 `BundleResLoader`、Catalogue、CDN 运行时 |
| `BaseEventSys/` | 事件总线 |
| `BaseCommandSys/` | 调试命令 |
| `BaseFSM/`、`BaseNetwork/`、`BaseSerialization/`、`Log/` | 规划或已有基础设施 |

**AOT 层只提供接口与管道**，例如：

- `IGameBootstrap` — 热更层实现并传入 `GameRoot.TryStart`
- `IGameModule` / `IServiceRegistry` — 热更层注册具体 Module

---

## 3. 不应放在 BaseFramework 的内容（热更层）

| 产物 | 建议目录 |
|------|----------|
| `GameBootstrap`（`Configure` 注册 Module 列表） | `Assets/HotUpdateScripts/` 或 `HotUpdateLayer/` |
| `HotUpdateGameEntry` / `AppEntry`（`TryStart(new GameBootstrap())`） | 同上 |
| 玩法 Module、Service、Proxy、Controller | 同上 |
| 配表 `*Meta` / `*Table` / `GameConfigTables` | `Assets/HotUpdateScripts/MetaConfigs/`（见 ConfigTableLayer 契约） |

### 3.1 关于 `HotUpdateBootStrap/`

当前 `BaseGameRoot/HotUpdateBootStrap/` 内仍有 **临时** 的 `GameBootstrap`、`HotUpdateGameEntry`（Editor 联调用）。

- **定位**：过渡代码，**不属于** AOT 长期内容  
- **目标**：迁入热更程序集后，BaseFramework 仅保留 `HotfixLaunchCoordinator` / `HybridCLRLoader` 与 `GameLaunchRunner`（Editor 模拟）

---

## 4. HybridCLR 与启动

1. **AOT（本目录）**：`GameRoot`、`HybridCLR` 加载器、`GameLaunchRunner`（Editor）  
2. **Load 热更 DLL** 后，调用 **热更程序集** 内入口（推荐 **直接静态调用**，HybridCLR 桥接；过渡期可用 `HotfixLaunchCoordinator` 反射）  
3. **热更入口** 内：`GameRoot.TryStart(new GameBootstrap())`

详见 [BaseGameRoot/README.md](BaseGameRoot/README.md) §4.3。

---

## 5. 与 BaseLayer 的区分

| 层级 | 路径 | 本仓库约定 |
|------|------|------------|
| **BaseFramework** | `Assets/vFramework/BaseFramework/` | **全部 AOT**（本文档） |
| **BaseLayer** | `Assets/vFramework/BaseLayer/` | 全局 Manager（ConfigTable、Input 等）；程序集划分随 asmdef 落地时单独文档化，**默认不视为热更 DLL 内容** |
| **热更业务** | `HotUpdateScripts/`、`HotUpdateLayer/` 等 | HybridCLR 热更 |

依赖方向不变：**热更 → BaseLayer → BaseFramework**（禁止反向引用）。

---

## 6. 相关文档

| 文档 | 内容 |
|------|------|
| [FrameworkDesign.md](../Docs/FrameworkDesign.md) | 三层架构总览 |
| [BaseGameRoot/README.md](BaseGameRoot/README.md) | GameRoot、TryStart、GameLaunch |
| [ConfigTableLayer/配表工具生成契约.md](../BaseLayer/ConfigTableLayer/配表工具生成契约.md) | 配表代码/数据目录 |
