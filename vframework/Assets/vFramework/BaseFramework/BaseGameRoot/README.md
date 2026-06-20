# BaseGameRoot 模块说明

> 路径：`BaseFramework/BaseGameRoot/`（**AOT 固定层**，见 [BaseFramework/README.md](../README.md)）  
> Bootstrap Scene 唯一 Mono 入口、IOC 服务容器、模块 Update 调度。

---

## 1. 职责

| 组件 | 职责 |
|------|------|
| **GameRoot** | 单例 Mono；`Awake` 占位 → 热更后 `TryStart` 装配 → 三相位 Update → `OnDestroy` 释放 |
| **GameBootstrapRegistry** | 持有 `IGameBootstrap` 实例（由 `TryStart` 写入） |
| **ServiceContainer** | 接口 → 单例实例映射 |
| **IoC** | 静态门面，委托 `ServiceContainer` |
| **ModuleManager** | 按 `Priority` 排序，`InitAll` / Update / FixedUpdate / LateUpdate / Editor Gizmo / `DisposeAll` |
| **GameTimeModule** | 内置 Clock / 双时刻 / Timer / UpdateFacade / Pipeline（`ModulePriority.Early`） |
| **GameFlowModule** | 宏观游戏流程 FSM（Boot / 主菜单 / …）；`IGameFlowService`（`ModulePriority.GameFlow`） |
| **IGameBootstrap** | 业务装配：`Register` Service + `AddModule`（**热更层实现**，不在 BaseFramework 长期驻留） |

本目录只提供 **AOT 框架骨架**；玩法 Module / Service 在热更层实现，通过 **`GameRoot.TryStart`** 接入（路径 B，对标 TEngine `GameApp.Entrance`）。

> `HotUpdateBootStrap/` 内 `GameBootstrap`、`HotUpdateGameEntry` 为 **过渡联调代码**，目标迁入 `HotUpdateScripts/` 等热更目录（见 [BaseFramework/README.md §3.1](../README.md)）。

---

## 2. 架构

### 2.1 无热更（AOT 最小运行）

```text
Bootstrap Scene：GameRoot + GameLaunchRunner（launchMode = AotBootstrap）
GameLaunchRunner.Awake (-9999)
    → GameRoot.TryStart(AotMinimalBootstrap)   // 无反射
GameRoot.StartPipeline
    → EnsureAssetSystemReady（集成 BaseAssetSys）
    → Configure（可选 GameTimeModule，无 GameFlow）
    → InitAll
```

### 2.2 热更路径（可选，HybridCLR）

```text
Bootstrap Scene：GameRoot + GameLaunchRunner（launchMode = HotfixReflection）
GameRoot.Awake → waitingBootstrap
GameLaunchRunner.Awake
    → HotfixLaunchCoordinator.TryLaunchGame()  // 反射仅解析一次并缓存
    → HotUpdateGameEntry.OnHotfixLoaded → TryStart(GameBootstrap)
    → Register + Configure + InitAll
```

热更**非必须**；无 HybridCLR 项目请用 §2.1 或自行 `TryStart(IGameBootstrap)`。

```text
GameRoot.Update / FixedUpdate / LateUpdate
    → IGameUpdatePipeline.RunFrame（若已注册 GameTimeModule）
    → ModuleManager（游戏时间 delta；无 Pipeline 时回退 Unity deltaTime）
```

```mermaid
sequenceDiagram
    participant Scene as BootstrapScene
    participant GR as GameRoot
    participant Hotfix as HotfixEntry
    participant BS as IGameBootstrap
    participant PL as IGameUpdatePipeline
    participant MM as ModuleManager

    Scene->>GR: Awake DontDestroyOnLoad
    Note over GR: waitingBootstrap
    Hotfix->>GR: TryStart(GameBootstrap)
    GR->>BS: Configure
    GR->>MM: InitAll
    loop each frame
        GR->>PL: RunFrame
        PL->>MM: Update gameDelta
    end
```

### 与 TEngine / GameFramework 对照

| 概念 | TEngine | vFramework |
|------|---------|------------|
| 热更入口 | `GameApp.Entrance` | **`GameRoot.TryStart`** |
| 业务装配 | `GameApp_RegisterSystem` | **`IGameBootstrap.Configure`** |
| 框架 Mono | Procedure 驱动 | **GameRoot** + **GameFlowModule** |
| Inspector 拖 Bootstrap | 无 | **无** |

---

## 3. 目录结构

```text
BaseGameRoot/
├── Bootstrap/
│   └── AotMinimalBootstrap.cs       ← 无热更时的最小 IGameBootstrap
├── GameLaunch/
│   ├── README.md                    ← 启动协调：设计意图 + AOT/热更用法
│   ├── GameLaunchMode.cs
│   ├── HotfixLaunchCoordinator.cs   ← 可选：反射调热更入口（MethodInfo 缓存）
│   └── GameLaunchRunner.cs          ← AotBootstrap / HotfixReflection
├── HotUpdateBootStrap/              ← 过渡：目标迁入 HotUpdateScripts
│   ├── FlowStates/                  ← Boot / MainMenu 等 Procedure 占位
│   ├── GameBootstrap.cs
│   └── HotUpdateGameEntry.cs
├── GameRoot/
│   ├── GameRoot.cs
│   ├── Interface/
│   │   ├── IGameModule.cs
│   │   ├── IFixedUpdateModule.cs
│   │   ├── ILateUpdateModule.cs
│   │   ├── IGizmoDrawModule.cs
│   │   ├── IGizmoDrawSelectedModule.cs
│   │   ├── IGameBootstrap.cs
│   │   ├── IServiceRegistry.cs
│   │   └── IModuleRegistry.cs
│   └── Impt/
│       ├── GameBootstrapRegistry.cs
│       ├── ServiceContainer.cs
│       ├── ModuleManager.cs
│       ├── IoC.cs
│       └── ModulePriority.cs
├── GameFlow/
│   ├── GameFlowApi.md   宏观流程 API + 设计思想
│   ├── Interface/
│   ├── Impt/
│   ├── Events/
│   └── Interface/                   ← 框架内核；无内置 Procedure 实现
└── GameTime/
    ├── GameTimeApi.md   业务 API 参考（Clock / 双时刻 / Timer / Facade）
    ├── Interface/
    └── Impt/
```

命名空间：`BaseFramework.BaseGameRoot`。

---

## 4. 业务接入

> **纯单机 / 只热更资源**（无 HybridCLR）的完整步骤、目录建议、Mono 迁移 → [Docs/Guides/StandaloneAndResourceHotfixGuide.md](../../Docs/Guides/StandaloneAndResourceHotfixGuide.md)

### 4.1 步骤清单

| 步骤 | 无热更 | 启用 HybridCLR（可选） |
|------|--------|------------------------|
| 1 | 实现 `IGameBootstrap` 或使用 `AotMinimalBootstrap` | 热更程序集实现 `GameBootstrap` |
| 2 | `Configure` 里按需 `Register` / `AddModule` | 同左 |
| 3 | Scene 挂 **GameRoot** | 同左 |
| 4 | `GameLaunchRunner` **AotBootstrap**，或代码 `TryStart` | `HotfixReflection` 或 Launcher 调 `HotfixLaunchCoordinator.TryLaunchGame()` |

### 4.2 IGameBootstrap

```csharp
public sealed class GameBootstrap : IGameBootstrap
{
    public void Configure(IServiceRegistry services, IModuleRegistry modules)
    {
        modules.AddModule(new GameTimeModule()); // 可选

        modules.AddModule(new GameFlowModule(
            registerStates: reg =>
            {
                reg.Register(new BootFlowState());
                reg.Register(new MainMenuFlowState());
            },
            initialStateId: GameFlowIds.Boot)); // 可选；不需要宏观流程则省略

        modules.AddModule(new MyGameLogicModule());
    }
}
```

### 4.3 热更入口（可选）

**热更为附加能力**，非所有项目需要。启用时 AOT 仅通过反射调用热更入口（`MethodInfo` 缓存，避免重复反射）：

```csharp
HotfixLaunchCoordinator.TryLaunchGame();
// → HotUpdateGameEntry.OnHotfixLoaded() → GameRoot.TryStart(new GameBootstrap())
```

| 场景 | 用法 |
|------|------|
| 单机 / 无 HybridCLR | `GameLaunchRunner` → **AotBootstrap**，或 `TryStart(new AotMinimalBootstrap())` |
| Editor 模拟热更 | `GameLaunchRunner` → **HotfixReflection**（需手动切换 launchMode） |
| 正式 HybridCLR | Launcher：`LoadAssembly` → `HotfixLaunchCoordinator.TryLaunchGame()` |

`GameRoot.Start` 会 **延后一帧** 检查是否已 TryStart，避免与 Launch Runner 的 Awake 竞态。

**GameLaunch 设计意图、时序图、`autoLaunchOnAwake` 自定义 Bootstrap、热更入口约定与踩坑** → [GameLaunch/README.md](GameLaunch/README.md)

- `TryStart` 成功：Register → `Configure` → `InitAll`
- `GameRoot` 尚未 Awake：仅 Register，Awake 时自动 `StartPipeline`
- 已启动后再调 `TryStart`：LogError，返回 false
- 首帧 `Start` 仍未启动：LogError 并 `enabled = false`

### 4.4 Module 取依赖

```csharp
public void Init(IServiceRegistry services)
{
    _input = services.Get<IInputService>();
    _gameplay = services.Get<IGameplayService>();
}
```

### 4.5 依赖注入约定

| 阶段 | 推荐 | 避免 |
|------|------|------|
| `Configure` | `Register` / `AddModule` | `IoC.Get` |
| `Init` | `services.Get` 赋字段 | Update 内 Get |
| `Update` 等 | 用缓存字段 | 热路径 `IoC.Get` |

---

## 5. 场景挂载

1. Bootstrap Scene：唯一 GameObject 挂 **GameRoot**。
2. **不要**在 Inspector 拖 Bootstrap 引用。
3. 热更流程在适当时机调用 **`GameRoot.TryStart`**。

---

## 6. API

### 6.1 GameRoot

| 成员 | 说明 |
|------|------|
| `Instance` | 全局单例 |
| `IsStarted` | 是否已完成 Configure / InitAll |
| `TryStart(IGameBootstrap)` | **热更入口**；注册并启动管道 |
| `Services` / `ModuleManager` | 启动后只读 |

### 6.2 GameBootstrapRegistry

| 方法 | 说明 |
|------|------|
| `Register` | 写入 Bootstrap（`TryStart` 内部调用） |
| `TryGet` | GameRoot Awake 时尝试读取 |

### 6.3 IGameModule / 可选相位 / ModulePriority

| 接口 | 调度 |
|------|------|
| `IGameModule` | `Update`（必选） |
| `IFixedUpdateModule` | `FixedUpdate` |
| `ILateUpdateModule` | `LateUpdate` |
| `IGizmoDrawModule` | Editor `OnDrawGizmos` → Scene 视图 |
| `IGizmoDrawSelectedModule` | Editor `OnDrawGizmosSelected`（选中 GameRoot 时） |

| 常量 | 值 | 典型模块 |
|------|-----|----------|
| `Input` | 0 | InputModule |
| `Early` | 100 | GameTimeModule |
| `GameFlow` | 150 | GameFlowModule |
| `Normal` | 500 | ArchiveModule |
| `GameLogic` | 600 | 战斗 ECS |
| `Late` | 900 | DebugCommandModule |
| `UI` | 1000 | UIMgr |

### 6.4 IoC

`IoC.Get<T>()` 仅迁移/调试；Module `Init` 优先 `services.Get`。

### 6.5 Editor Scene Gizmo（IGizmoDrawModule）

Module 为纯 C#，Gizmo 只能由 **`GameRoot` Mono** 转发。实现可选接口后，在 **Play 且 TryStart 成功** 时，Scene 视图（Gizmos 开关打开）可见。

```csharp
public sealed class PathDebugModule : IGameModule, IGizmoDrawModule
{
    private Vector3[] _points;

    public int Priority => ModulePriority.GameLogic;

    public void Init(IServiceRegistry services) { /* 缓存依赖 */ }
    public void Update(float deltaTime) { /* 更新 _points */ }
    public void Dispose() { _points = null; }

    public void DrawGizmos()
    {
        if (_points == null || _points.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < _points.Length - 1; i++)
            Gizmos.DrawLine(_points[i], _points[i + 1]);
    }
}
```

| 注意 | 说明 |
|------|------|
| 回调内 | 只用 `Gizmos.*`，读 Init/Update 缓存，勿 `services.Get` |
| 未启动 | Edit Mode / 未 `TryStart` 时不绘制（`_started` 门禁） |
| 编译 | 调度代码仅 `#if UNITY_EDITOR`，Player 包无开销 |
| 选中细节 | 实现 `IGizmoDrawSelectedModule` 绘制额外线条 |

---

## 8. GameTime

内置子模块：游戏时钟、连续 + 日历双时刻、`ITimerService`（Delay / Repeat）、三相位 Update 门面。Bootstrap 中注册 `GameTimeModule` 后，`GameRoot` 经 `IGameUpdatePipeline` 驱动 Update / Fixed / Late。

**详细 API、示例与 Timer / Facade 选型** → [GameTime/GameTimeApi.md](GameTime/GameTimeApi.md)

```csharp
modules.AddModule(new GameTimeModule(new GameTimeOptions
{
    CalendarSettings = new GameCalendarSettings { SecondsPerDay = 120f }
}));
```

---

## 9. GameFlow

内置子模块：宏观游戏阶段（Boot → 主菜单 → …），单当前态 + `Enter/Update/Exit`，无 `MonoBehaviour`。Bootstrap 注册 `GameFlowModule` 后，业务实现 `IGameFlowState` 并在 Configure 中 `Register`；运行时经 `IGameFlowService` 查询与切换。

**详细 API、设计思想、新增状态步骤** → [GameFlow/GameFlowApi.md](GameFlow/GameFlowApi.md)

```csharp
modules.AddModule(new GameFlowModule(
    registerStates: reg => reg.Register(new ProcedureBattle())));

modules.AddModule(new DebugCommandModule(reg =>
    GameFlowModule.RegisterDebugCommands(reg)));
```

---

## 10. 扩展设计

| 阶段 | 内容 |
|------|------|
| Phase 1 ✅ | `IGameModule` + `ServiceContainer` + Update |
| Phase 2 ✅ | Fixed / Late 相位 |
| Phase 2.5 ✅ | GameTime 双时刻 + Pipeline |
| Phase 2.6 ✅ | GameFlow 宏观流程内核（Procedure 由 Bootstrap Register） |
| Phase 3 | `EcsWorldModule` |
| 启动 | **AotBootstrap**（无热更）或 **HotfixReflection**（可选 HybridCLR） |
