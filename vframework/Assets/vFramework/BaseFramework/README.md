# BaseFramework（AOT 固定层）

> 路径：`Assets/vFramework/BaseFramework/`  
> **本目录下全部代码属于 AOT 固定框架层**：随安装包编译进主程序集，**不**通过 HybridCLR 热更替换。

上层文档：[FrameworkDesign.md](../Docs/Overview/FrameworkDesign.md) §8、[ProjectGoals.md](../Docs/Overview/ProjectGoals.md)

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
| `BaseGameRoot/` | `GameRoot`、IOC、`ModuleManager`、`IGameBootstrap` 接口、**可选** GameTime / GameFlow 内核、GameLaunch |
| `BaseAssetSys/` | AB 打包与 `BundleResLoader`、Catalogue、CDN 运行时 |
| `BaseEventSys/` | 事件总线 |
| `BaseCommandSys/` | 调试命令 |
| `BaseFSM/`、`BaseNetwork/`、`BaseSerialization/`、`Log/` | 规划或已有基础设施 |

**AOT 层只提供接口与可选 Module 内核**：

- `IGameBootstrap` — 热更层或 `AotMinimalBootstrap` 实现并传入 `GameRoot.TryStart`
- `GameTimeModule` / `GameFlowModule` — Bootstrap 按需 `AddModule`；无 GameTime 时 GameRoot 回退 Unity `deltaTime`
- `IGameModule` / `IServiceRegistry` — 热更层注册具体 Module

---

## 3. 不应放在 BaseFramework 的内容（热更层）

| 产物 | 建议目录 |
|------|----------|
| `GameBootstrap`（`Configure` 注册 Module 列表） | `Assets/HotUpdateScripts/` 或 `HotUpdateLayer/` |
| `HotUpdateGameEntry` / `AppEntry`（`TryStart(new GameBootstrap())`） | 同上 |
| 玩法 Module、Service、Proxy、Controller | 同上 |
| 配表 `*Meta` / `*Table` / `GameConfigTables` | `Assets/HotUpdateScripts/MetaConfigs/`（见 [配表工具生成契约.md](../BaseLayer/ConfigTableLayer/配表工具生成契约.md)） |

### 3.1 关于 `HotUpdateBootStrap/`

当前 `BaseGameRoot/HotUpdateBootStrap/` 内仍有 **临时** 的 `GameBootstrap`、`HotUpdateGameEntry`（Editor 联调用）。

- **定位**：过渡代码，**不属于** AOT 长期内容  
- **目标**：迁入热更程序集后，BaseFramework 仅保留 `HotfixLaunchCoordinator` / `HybridCLRLoader` 与 `GameLaunchRunner`（Editor 模拟）

---

## 4. HybridCLR 与启动（可选）

**热更为附加能力**，非所有项目必须启用。无 HybridCLR 时使用 `AotMinimalBootstrap` + `GameLaunchMode.AotBootstrap` 即可稳定运行 AOT 骨架。

| 侧 | 职责 |
|----|------|
| **AOT（本目录）** | `GameRoot`（集成 Asset 预热）、`GameTimeModule` / `GameFlowModule` **内核**、`GameLaunchRunner` |
| **AOT 无热更** | `TryStart(AotMinimalBootstrap)`，无反射 |
| **启用热更** | `HotfixLaunchCoordinator`（反射入口 **仅解析一次并缓存**）→ 热更 `OnHotfixLoaded` → `TryStart` |

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
| [FrameworkDesign.md](../Docs/Overview/FrameworkDesign.md) | 三层架构总览 |
| [StandaloneAndResourceHotfixGuide.md](../Docs/Guides/StandaloneAndResourceHotfixGuide.md) | 单机 / 只热更资源接入 |
| [BaseGameRoot/README.md](BaseGameRoot/README.md) | GameRoot、TryStart、GameLaunch |
| [ConfigTableLayer/配表工具生成契约.md](../BaseLayer/ConfigTableLayer/配表工具生成契约.md) | 配表代码/数据目录 |
