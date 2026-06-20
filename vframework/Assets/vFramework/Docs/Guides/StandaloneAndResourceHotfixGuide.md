# 单机与资源热更新项目接入指南

> 路径：`Assets/vFramework/Docs/Guides/`  
> 适用：**纯单机**、**只热更资源（不热更 C# 逻辑）** 的 Unity 项目。  
> 前置：[ProjectGoals.md](../Overview/ProjectGoals.md)、[FrameworkDesign.md](../Overview/FrameworkDesign.md)

本文说明如何用 vFramework **启动游戏、注册业务、绑定依赖**，以及如何把旧项目里散落的 **MonoBehaviour 逻辑** 迁入 Module / Service / GameFlow。  
**不需要 HybridCLR** 即可完整使用本框架；资源 CDN 热更与代码热更是两条独立能力。

---

## 1. 两种常见模式对照

| 维度 | 纯单机 | 只热更资源 |
|------|--------|------------|
| **C# 逻辑** | 随 App 发版（AOT） | 同左，**不**走 HybridCLR |
| **资源（AB / 场景 / Prefab）** | 首包 `StreamingAssets` 即可 | 首包 + CDN 增量；运行时拉新 `catalog.bytes` / bundle |
| **启动方式** | `GameLaunchMode.AotBootstrap` | 同左 |
| **是否需要 `HotfixLaunchCoordinator`** | **否** | **否** |
| **Bootstrap 放哪** | 项目程序集（见 §2） | 同左 |
| **Patch 流程** | 可省略 | Boot 阶段检查版本 / 拉清单（见 §6） |

```text
                    ┌─────────────────────────────────────┐
                    │  GameRoot + GameLaunchRunner        │
                    │  launchMode = AotBootstrap          │
                    └─────────────────┬───────────────────┘
                                      │ TryStart(你的 IGameBootstrap)
                                      ▼
                    ┌─────────────────────────────────────┐
                    │  EnsureAssetSystemReady             │
                    │  Configure → InitAll → Update       │
                    └─────────────────┬───────────────────┘
                                      │
          ┌───────────────────────────┼───────────────────────────┐
          ▼                           ▼                           ▼
   GameFlowModule              SceneModule                   你的 GameLogicModule
   (Boot→主菜单→局内)           (LoadSingleAsync)              (玩法 Tick)
```

**代码热更（HybridCLR）** 不在本文范围；若后续需要，见 [GameLaunch/README.md](../../BaseFramework/BaseGameRoot/GameLaunch/README.md) 与 [FrameworkDesign.md §8](../Overview/FrameworkDesign.md)。

---

## 2. 项目目录与程序集建议

框架三层（BaseFramework → BaseLayer → 业务）不变；**业务代码不要写进 `BaseFramework/`**。

### 2.1 推荐布局（无代码热更）

```text
Assets/
├── vFramework/                    # 框架（已有）
├── GameScripts/                   # ★ 你的项目业务（新建 asmdef）
│   ├── Bootstrap/
│   │   └── GameBootstrap.cs       # 实现 IGameBootstrap
│   ├── Modules/
│   │   └── BattleLogicModule.cs   # 实现 IGameModule
│   ├── Services/
│   │   └── PlayerProgressService.cs
│   ├── FlowStates/
│   │   └── MainMenuFlowState.cs
│   └── View/                      # 薄 MonoBehaviour（UI 绑定）
│       └── HudView.cs
├── HotUpdateScripts/Config/       # SceneCatalog、配表 asset 等（可选）
└── Init.unity                     # Bootstrap Scene：GameRoot + GameLaunchRunner
```

| 内容 | 放哪 | 原因 |
|------|------|------|
| `IGameBootstrap` 实现 | `GameScripts/Bootstrap/` | 项目专属装配，不属于框架 |
| 玩法 Module / Service | `GameScripts/` | 随 App 编译，清晰边界 |
| `SceneCatalog.asset` | `HotUpdateScripts/Config/` 或 `Settings/Game/` | 见 [SceneLayer/README.md §1.1](../../BaseLayer/SceneLayer/README.md) |
| 框架内核 | `vFramework/BaseFramework/` | **禁止**改放业务 |

创建程序集定义 `GameScripts.asmdef`，引用 `BaseFramework`、`BaseLayer`（及用到的模块程序集）。  
目录名 `HotUpdateScripts` **不强制**表示 HybridCLR——只是与配置资源同级的惯例命名。

### 2.2 与过渡代码的关系

仓库内 `BaseLayer/HotUpdateBootStrap/` 的 `GameBootstrap` 是 **Editor 联调模板**。  
正式项目应 **复制模式到 `GameScripts/` 并改 namespace**，而不是长期改框架目录里的文件。

---

## 3. 启动：从零到跑起来

### 3.1 Bootstrap Scene

1. 打开或创建 Bootstrap Scene（如 `Init.unity`）。  
2. 创建 GameObject，挂 **GameRoot**（`DontDestroyOnLoad` 由脚本处理）。  
3. 同物体挂 **GameLaunchRunner**：  
   - `launchMode` = **AotBootstrap**  
   - `autoLaunchOnAwake` = **true**（默认；自定义启动见 §3.4）  
4. Build Settings 把该 Scene 置于 **Index 0**。

GameRoot 会在 `TryStart` 时自动 `EnsureAssetSystemReady`（读 `catalog.bytes`），无需在 Bootstrap 里单独 Init 资源系统。

### 3.2 实现 IGameBootstrap

在 `GameScripts/Bootstrap/GameBootstrap.cs`：

```csharp
using BaseFramework.BaseGameRoot;
using BaseLayer.Scene;
using BaseLayer.Scene.Impt;
using UnityEngine;

namespace MyGame.Bootstrap
{
    public sealed class GameBootstrap : IGameBootstrap
    {
        readonly SceneCatalog _sceneCatalog;

        // 推荐：Bootstrap Scene 上某 Mono 构造时传入，或 Resources.Load
        public GameBootstrap(SceneCatalog sceneCatalog) => _sceneCatalog = sceneCatalog;

        public void Configure(IServiceRegistry services, IModuleRegistry modules)
        {
            // [100] 游戏时间 / Timer（建议保留）
            modules.AddModule(new GameTimeModule());

            // [140] 场景调度（若用 SceneLayer）
            if (_sceneCatalog != null)
                modules.AddModule(new SceneModule(_sceneCatalog));

            // [150] 宏观流程（Boot → 主菜单 → 局内）
            modules.AddModule(new GameFlowModule(
                registerStates: reg =>
                {
                    reg.Register(new BootFlowState());
                    reg.Register(new MainMenuFlowState());
                    // reg.Register(new InGameFlowState());
                },
                initialStateId: GameFlowIds.Boot));

            // [600] 核心玩法 Module
            modules.AddModule(new BattleLogicModule());

            // 纯数据 / 无 Tick 的能力 → Service
            services.Register(new PlayerProgressService());
        }
    }
}
```

### 3.3 替换默认 AotMinimalBootstrap

默认 `GameLaunchRunner` 会 `TryStart(new AotMinimalBootstrap())`，只含 `GameTimeModule`。  
接入自己的 Bootstrap 有两种方式：

**方式 A — 改 Runner 源码（仅本地原型）**  
不推荐长期维护；适合快速验证。

**方式 B — 关自动启动，自行 TryStart（推荐）**

```csharp
// 挂在 Bootstrap Scene，DefaultExecutionOrder 早于 -9999 亦可
public sealed class GameEntry : MonoBehaviour
{
    [SerializeField] SceneCatalog sceneCatalog;

    void Awake()
    {
        GameRoot.TryStart(new GameBootstrap(sceneCatalog));
    }
}
```

Inspector：`GameLaunchRunner.autoLaunchOnAwake = false`（项目 `Init.unity` 已是此配置）。

### 3.4 启动时序（避免踩坑）

| 顺序 | 行为 |
|------|------|
| `GameRoot.Awake` (-10000) | 单例 + `DontDestroyOnLoad` |
| 你的 `TryStart` / `GameLaunchRunner.Awake` (-9999) | Register Bootstrap → `StartPipeline` |
| `StartPipeline` | Asset 预热 → `Configure` → `InitAll` |
| `GameRoot.Start` | 若仍无 Bootstrap，**延后一帧** LogError |

`TryStart` **只能成功一次**；重复调用会 LogError。

---

## 4. 注册与绑定业务

框架采用 **Composition Root + 构造期注入**：

- **`IGameBootstrap.Configure`**：唯一注册 Service、Module 的地方。  
- **`IGameModule.Init`**：从 `IServiceRegistry` **Get 一次**，赋给字段缓存。  
- **Update / 热路径**：用缓存字段，**避免** `IoC.Get<T>()`。

### 4.1 Module vs Service

| 类型 | 何时用 | 接口 | 生命周期 |
|------|--------|------|----------|
| **Module** | 需要每帧或固定相位 Tick | `IGameModule`（可选 `IFixedUpdateModule` / `ILateUpdateModule`） | `Init` → `Update*` → `Dispose` |
| **Service** | 无 Tick 的能力（存档、配置门面、进度） | 自定义接口 + `services.Register` | 随 GameRoot `OnDestroy` 清空容器 |

```csharp
public sealed class BattleLogicModule : IGameModule
{
    ISceneService _scene;
    IGameFlowService _flow;
    PlayerProgressService _progress;

    public int Priority => ModulePriority.GameLogic;

    public void Init(IServiceRegistry services)
    {
        _scene = services.Get<ISceneService>();
        _flow = services.Get<IGameFlowService>();
        _progress = services.Get<PlayerProgressService>();
    }

    public void Update(float deltaTime) { /* 局内规则 */ }
    public void Dispose() { }
}
```

`ModulePriority` 见 [BaseGameRoot/README.md §6.3](../../BaseFramework/BaseGameRoot/README.md)。

### 4.2 宏观流程（GameFlow）

用 **GameFlow** 表达「Boot / 主菜单 / 局内 / 结算」，**不要**用多个 `DontDestroyOnLoad` Mono 切换阶段。

```csharp
public sealed class MainMenuFlowState : IGameFlowState
{
    public string Id => GameFlowIds.MainMenu;

    public void Enter(IGameFlowContext context)
    {
        // 异步加载菜单场景（勿直接 SceneManager）
        _ = context.Services.Get<ISceneService>().LoadSingleAsync("MainMenu");
    }

    public void Update(float deltaTime, IGameFlowContext context) { }
    public void Exit(IGameFlowContext context) { }
}
```

API 详见 [GameFlow/GameFlowApi.md](../../BaseFramework/BaseGameRoot/GameFlow/GameFlowApi.md)。

### 4.3 场景与资源

| 能力 | 入口 | 禁止 |
|------|------|------|
| 切场景 | `ISceneService.LoadSingleAsync` / `LoadAdditiveAsync` | 业务直接 `SceneManager.LoadScene` |
| 加载 Prefab | `BundleResLoader.Instance.Load<T>(loadPath)` | 写 `Assets/...` 磁盘路径 |
| 跨模块通知 | `GameEventBus` 或 Model 事件 | 在 Update 里 `FindObjectOfType` |

场景配置见 [SceneLayer/README.md](../../BaseLayer/SceneLayer/README.md)；资源 API 见 [BusinessApiUsageGuide.md](../../BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md)。

### 4.4 View 层（保留 MonoBehaviour 的位置）

UI / 动画 / 粒子等 **与 Unity 组件强绑定** 的脚本仍可继承 `MonoBehaviour`，但应 **变薄**：

```text
玩家点击 → View 事件 → Controller / FlowState / Module 方法
数据刷新 ← Model 或 EventBus ← Service / Proxy（若有网）
```

View **不**注册进 `ServiceContainer`；通过 Inspector 引用或父级传入，**不**在 View 的 `Update` 里写玩法规则。

### 4.5 联机（可选）

单机可跳过。若以后要联机，在 `Configure` 注册 NetMgr 相关 Module，玩法用 **Proxy 写 Model、Controller 读 Model**（[FrameworkDesign.md §6–7](../Overview/FrameworkDesign.md)）。

---

## 5. 只热更资源：Patch 与运行时

逻辑仍在 AOT；**变更的是 `catalog.bytes` 与 AB 文件**。BaseAssetSys 阶段 C 已支持 CDN 清单同步与按需下载 bundle。

### 5.1 打包侧

| 产出 | 用途 |
|------|------|
| `StreamingAssets/{平台}/` | 首包内置 catalog + 必要 AB |
| `Bundles/CDN/{平台}/` | CI 上传 CDN 的增量 / 全量 AB |
| 清单字段 `cdnBaseUrl` | 运行时知道去哪拉资源 |

详见 [BusinessApiAndCdnPlanning.md §2](../../BaseFramework/BaseAssetSys/Docs/BusinessApiAndCdnPlanning.md)。

### 5.2 运行时查找顺序

```text
persistentDataPath / ABCache     ← CDN 已下载的新版 bundle + 清单
    ↓ 未命中
StreamingAssets / 首包
    ↓ 未命中
CDN HTTP 下载 → 写入 ABCache → 再 Load
```

`BundleResLoader.Init` / `EnsureReady` 在清单含 `cdnBaseUrl` 时会自动：`CdnCatalogueSyncService` 对比 `catalogueHash`，必要时拉新清单；本地无包时走 `HttpRemoteBundleProvider`。

业务侧 **一般无需** 手写 `UnityWebRequest` 下 AB。

### 5.3 推荐 Patch 流程（BootFlowState）

在 **GameFlow Boot 态**（`TryStart` 已完成、Asset 系统已预热）做版本检查与可选预下载：

```csharp
public sealed class BootFlowState : IGameFlowState
{
    public string Id => GameFlowIds.Boot;

    public async void Enter(IGameFlowContext context)
    {
        // 1. EnsureReady 已在 GameRoot.StartPipeline 完成
        // 2. 可选：预下载关键 bundle（见 BusinessApiUsageGuide §1 PreLoadBundles）
        await BundleResLoader.Instance.PreLoadBundlesAsync(new[] { "common.bundle" });

        // 3. 进入主菜单或首屏
        context.Flow.ChangeState(GameFlowIds.MainMenu);
    }
    // ...
}
```

若 Patch UI 必须在 **TryStart 之前** 显示（如全屏下载条）：

1. `GameLaunchRunner.autoLaunchOnAwake = false`  
2. Patch Mono 完成下载 / 校验  
3. `GameRoot.TryStart(new GameBootstrap(...))`

### 5.4 GameRoot 与 AB 根路径

| Inspector 字段 | 用途 |
|----------------|------|
| `bundleRootOverride` | 空 = 默认 `StreamingAssets/{平台}/`；可指向 `persistentDataPath` 下缓存根 |
| `usePlatformSubfolder` | 是否在根路径下再追加平台子目录 |

只热更资源的项目通常 **留空** `bundleRootOverride`，依赖 `DefaultBundlePathResolver` 的 **ABCache → 首包** 优先级即可。

### 5.5 与代码热更的边界

| 可热更（资源） | 不可热更（需发 App） |
|----------------|----------------------|
| Prefab、场景、贴图、音频 AB | `IGameModule`、Service、GameFlow 状态类 |
| `catalog.bytes`、远程 bundle | `GameBootstrap.Configure` 的 C# 注册列表 |
| ScriptableObject 配置（若打进 AB） | 新增 `msgId` 协议处理（除非另有热更 DLL） |

---

## 6. MonoBehaviour 迁移指南

旧项目常见问题是：**每个系统一个 Singleton Mono + Update**。迁移目标：**一个 GameRoot 驱动多 Module**，View 留 Mono。

### 6.1 决策表

| 旧代码特征 | 迁到哪里 | 说明 |
|------------|----------|------|
| `Update` / `FixedUpdate` 驱动玩法 | `IGameModule` | 注册到 Bootstrap，`Init` 里 Get 依赖 |
| `Awake`/`Start` 里 `Init()` 一次 | `IGameModule.Init` 或 `IGameFlowState.Enter` | 按是否「阶段相关」选择 |
| `static Instance` 管理器 | `Service` + `services.Register` | 去掉 lazy singleton |
| `DontDestroyOnLoad` 常驻 Mono | **仅保留 GameRoot** | 其他逻辑进 Module |
| 场景加载 `SceneManager.LoadScene` | `ISceneService` + GameFlow 切换 | Single 前自动清理链 |
| `Coroutine` 异步 | `UniTask`（Module / Service 内） | 项目已引用 UniTask |
| UI 按钮、Animator | 薄 **View** Mono | 转发到 Controller / Module 公共方法 |
| 纯工具静态类 | 保持 static 或 Service | 无 Unity 生命周期则不必 Module |

### 6.2 迁移步骤（建议顺序）

1. **Bootstrap Scene**：挂 GameRoot + GameLaunchRunner（或 `GameEntry` TryStart）。  
2. **写一个 `GameBootstrap`**：先把现有 Manager **原样包一层 Service** 注册进去，Module 可先只加 `GameTimeModule`。  
3. **逐个 Manager → Module 或 Service**：删掉 `Instance` getter 与 `DontDestroyOnLoad`。  
4. **用 GameFlow 替换「状态 Mono」**：如 `MainMenuController` + `GameController` 合并为 FlowState。  
5. **场景入口**：Bootstrap 只留 GameRoot；菜单 / 局内场景由 `ISceneService` 加载。  
6. **View 瘦身**：Mono 只留序列化字段与 UI 回调。

### 6.3 对照示例

**迁移前（反模式）**

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    void Awake() { Instance = this; DontDestroyOnLoad(gameObject); }
    void Update() { TickGameplay(Time.deltaTime); }
}
```

**迁移后**

```csharp
// GameBootstrap.Configure:
modules.AddModule(new GameplayModule());

public sealed class GameplayModule : IGameModule
{
    public int Priority => ModulePriority.GameLogic;
    public void Init(IServiceRegistry services) { }
    public void Update(float deltaTime) => TickGameplay(deltaTime);
    public void Dispose() { }
}
```

**迁移前：场景里挂 `LevelLoader` Mono**

```csharp
void Start() { SceneManager.LoadScene("Level1"); }
```

**迁移后：BootFlowState 或 MainMenuFlowState**

```csharp
await _scene.LoadSingleAsync("Level1");
```

### 6.4 暂时无法删掉的 Mono

| 情况 | 做法 |
|------|------|
| 第三方插件必须挂 Scene | 保留在业务 Scene；通过 EventBus 或 Service 与 Module 通信 |
| 编辑器调试工具 | `#if UNITY_EDITOR` 或独立 Scene，不进 Player 首包流程 |
| 相机 / 灯光 / Timeline | 场景内容，不是「系统 Manager」 |

### 6.5 请勿

- 在 Module 里 `new GameObject` 再挂第二个「小 GameRoot」。  
- 在 `Configure` 里 `IoC.Get`（容器尚未完成 InitAll）。  
- 多个脚本各自 `TryStart` 不同 Bootstrap。  
- 业务代码引用 `HybridCLR` / `HotfixLaunchCoordinator`（单机不需要）。

---

## 7. 最小闭环 Checklist

- [ ] Bootstrap Scene：`GameRoot` + `GameLaunchRunner`（AotBootstrap）或 `GameEntry.TryStart`  
- [ ] `GameScripts.asmdef` 引用 BaseFramework / BaseLayer  
- [ ] `GameBootstrap.Configure` 注册 GameTime、（可选）Scene、GameFlow、玩法 Module  
- [ ] 业务 Module 在 `Init` 中 `Get` 依赖并缓存  
- [ ] 场景切换走 `ISceneService`；资源走 `BundleResLoader` + `loadPath`  
- [ ] 仅资源热更：清单配置 `cdnBaseUrl`；Boot 态 PreLoad / 进菜单  
- [ ] 旧 Singleton Mono 已改为 Module 或 Service  

---

## 8. 相关文档

| 文档 | 内容 |
|------|------|
| [GameLaunch/README.md](../../BaseFramework/BaseGameRoot/GameLaunch/README.md) | 启动模式、`autoLaunchOnAwake` |
| [BaseGameRoot/README.md](../../BaseFramework/BaseGameRoot/README.md) | TryStart、ModulePriority、IoC 约定 |
| [GameFlow/GameFlowApi.md](../../BaseFramework/BaseGameRoot/GameFlow/GameFlowApi.md) | 宏观流程状态机 |
| [SceneLayer/README.md](../../BaseLayer/SceneLayer/README.md) | SceneCatalog、Single/Additive |
| [BusinessApiUsageGuide.md](../../BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md) | Load / Release / PreLoad / CDN |
| [BusinessApiAndCdnPlanning.md](../../BaseFramework/BaseAssetSys/Docs/BusinessApiAndCdnPlanning.md) | CDN 设计与路径优先级 |
| [FrameworkDesign.md §6–7](../Overview/FrameworkDesign.md) | MVC + Proxy（联机扩展） |
| [Docs/README.md](../README.md) | 文档总索引 |
