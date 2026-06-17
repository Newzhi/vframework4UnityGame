# vFramework for Unity

一个 **通用、轻量** 的 Unity 游戏框架练习仓库。  
不绑定品类，不承诺能吊打大厂，但能帮你把「启动 → 模块 → 资源 → 玩法」这条线捋顺一点。

> **免责声明（请大声朗读三遍）**  
> 这是 **小孩子不懂事写着玩的**。  
> 架构师看了可能沉默，程序员看了可能扶额，产品经理看了会问「所以什么时候能上线」。  
> API、目录、命名随时 **原地重构、连夜跑路、赛博火化**——**请勿**当商业框架直接上生产；参考、fork、魔改随意，**炸了别@我**。

---

## 三层是啥（人话版）

```text
HotUpdateLayer     你的玩法、MVC、Proxy（爱写啥写啥）
       ↓
BaseLayer          UI / 音频 / 输入 / 存档 … 全局管家
       ↓
BaseFramework      发动机：GameRoot、事件、资源管线、调试命令 …
```

原则就几条：**依赖往下走**、**Manager 别到处 new**、**资源用逻辑路径别写 Assets 全路径**、需要联机再让 Proxy 接网络。

---

## 有哪些模块

### BaseFramework（基础设施）

| 模块 | 干啥的 | 你怎么碰它 |
|------|--------|------------|
| **BaseGameRoot** | 唯一入口 `GameRoot`、IOC、`ModuleManager` 三相位 Update | 场景挂 `GameRoot`，热更/业务里 `GameRoot.TryStart(bootstrap)` |
| **GameFlow** | 宏观流程：Boot、主菜单、进局内…（对标 Procedure） | `IGameFlowService.ChangeState("MainMenu")` |
| **GameTime** | 游戏时钟、Timer、Fixed/Late 门面 | 注册 `GameTimeModule`，用 `IGameTimeClock` / `ITimerService` |
| **BaseEventSys** | 类型安全事件总线 | `GameEventBus.RegisterEvent` / `SentEvent` |
| **BaseAssetSys** | AB 打包（Editor）+ 运行时 `BundleResLoader` + 对象池 | `BundleResLoader.Instance.Load<T>("path")` |
| **BaseCommandSys** | 调试文本命令（可接 MCP） | `ICommandDispatcher.Execute("help")` |
| **BaseLogSys** | 调试日志 | `DebugLogger` 等 |
| **BaseNetSys** | 网络底层雏形 | 业务协议放 HotUpdate 的 Proxy，别在这里写玩法 |

### BaseLayer（全局系统）

| 模块 | 干啥的 | 你怎么碰它 |
|------|--------|------------|
| **InputLayer** | 每帧输入快照（键鼠 / 触摸） | Bootstrap 里 `AddModule(new InputModule())`，`Get<IInputService>()` |
| **ArchiveLayer** | 存档槽位 CRUD | `AddModule(new ArchiveModule(...))`，`Get<IArchiveService>()` |
| **AudioLayer** | 音频管理 | `IAudioManager` |
| **UILayer** | 窗口基类 `UIWindow` 等 | 继承窗口、走 UI 栈（见模块内代码） |
| **SceneLayer** | 场景调度 | `SceneScheduler` |

此外还有 **ConfigTableLayer**、**I18NLayer**、**GameUtils** 等辅助目录；**HotUpdateLayer** 留给业务自己填 MVC / Proxy。

测试和 Demo 在 `Assets/Test/`、`Assets/AssetBundle/`，和框架核心分开放——别在框架里找「完整游戏」，找到了也可能是幻觉。

---

## 怎么用（最短路径）

### 1. 打开工程

```bash
git clone https://github.com/<你的用户名>/vFramework.git
```

Unity Hub 打开 **`vframework/`** 文件夹（不是仓库根目录）。  
版本：**Unity 2022.3 LTS**（当前 `2022.3.62f3c1`）。

### 2. 启动链路

```text
Bootstrap 场景挂 GameRoot（DontDestroyOnLoad）
    → 业务/热更加载完
    → GameRoot.TryStart(new YourGameBootstrap())
    → IGameBootstrap.Configure：Register 服务 + AddModule
    → ModuleManager.InitAll
    → 每帧 Update / FixedUpdate / LateUpdate
```

`YourGameBootstrap` 里典型几行：

```csharp
public void Configure(IServiceRegistry services, IModuleRegistry modules)
{
    modules.AddModule(new GameTimeModule());
    modules.AddModule(new GameFlowModule());
    modules.AddModule(new InputModule());
    modules.AddModule(new DebugCommandModule(/* 注册命令 */));
    // 你的玩法 Module、Proxy 注册也放这里
}
```

### 3. 拿服务

```csharp
var flow = IoC.Get<IGameFlowService>();
flow.ChangeState("MainMenu");

var input = IoC.Get<IInputService>();
if (input.Current.Attack.PressedThisFrame) { /* 开火 */ }
```

### 4. 加载资源

```csharp
BundleResLoader.Instance.EnsureReady();
var handle = BundleResLoader.Instance.Load<GameObject>("ui/main_menu");
var go = handle.GetAsset<GameObject>();
// 用完记得 Release，别当慈善家
```

Editor 下可先打 AB（菜单见 `BaseAssetSys/Editor`），或走 Resources / Editor 模拟路径——细节见资源文档。

### 5. 发事件（解耦用，别当每帧弹幕）

```csharp
GameEventBus.RegisterEvent<MyEvent>(OnMyEvent);
GameEventBus.SentEvent(new MyEvent { Value = 42 });
GameEventBus.DeRegisterEvent<MyEvent>(OnMyEvent);  // 记得退订，否则内存泄漏会来找你
```

---

## 依赖啥

### 环境

| 项 | 说明 |
|----|------|
| **Unity** | 2022.3 LTS |
| **渲染** | URP（`com.unity.render-pipelines.universal`） |

### 主要第三方包（`Packages/manifest.json`）

| 包 | 用途 |
|----|------|
| **UniTask** | 异步（`com.cysharp.unitask`） |
| **Addressables** | 资源寻址与加载链一环 |
| **TextMeshPro / uGUI** | UI |
| **Unity Web Request 等模块** | CDN / AB 下载 |

框架通过 **asmdef** 分层引用：业务层别直接 `using` 一堆第三方，尽量走接口——这样以后换库时你还能假装没事发生。

---

## 仓库结构

```text
vFramework/
├── README.md          ← 你正在看
├── LICENSE            ← MIT，开源但无保修
└── vframework/        ← Unity 工程，Hub 开这个
    └── Assets/vFramework/
        ├── Docs/              框架总文档
        ├── BaseFramework/     基础设施
        ├── BaseLayer/         全局系统
        └── HotUpdateLayer/    业务（你来写）
```

---

## 文档导航

| 文档 | 内容 |
|------|------|
| [FrameworkDesign.md](vframework/Assets/vFramework/Docs/FrameworkDesign.md) | 分层、数据流、约定 |
| [ProjectGoals.md](vframework/Assets/vFramework/Docs/ProjectGoals.md) | 定位与范围 |
| [BaseGameRoot/README.md](vframework/Assets/vFramework/BaseFramework/BaseGameRoot/README.md) | 入口与 Module |
| [GameFlowApi.md](vframework/Assets/vFramework/BaseFramework/BaseGameRoot/GameFlow/GameFlowApi.md) | 宏观流程 |
| [BaseEventSys/README.md](vframework/Assets/vFramework/BaseFramework/BaseEventSys/README.md) | 事件总线 |
| [BusinessApiUsageGuide.md](vframework/Assets/vFramework/BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md) | 资源加载 API |
| [DocumentIndex.md](vframework/Assets/vFramework/BaseFramework/BaseAssetSys/Docs/DocumentIndex.md) | 资源域文档索引 |

---

## 参与与反馈

欢迎 Issue / PR。合并节奏随缘，大改前建议先开 Issue——不然可能出现「我昨晚梦见更好的架构」式 PR。

---

## 许可

**开源发布**，[MIT License](LICENSE)。

可自由使用、修改、分发；保留版权声明即可。  
**无任何担保**——尤其当作者声称「这次应该稳了」的时候，请保持怀疑。
