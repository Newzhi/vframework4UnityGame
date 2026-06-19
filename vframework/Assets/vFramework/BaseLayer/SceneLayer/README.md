# SceneLayer

> 路径：`BaseLayer/SceneLayer/`  
> 命名空间：`BaseLayer.Scene`

安全加载/卸载 Unity 场景（Build-in / AssetBundle），Single / Additive，切换前自动执行资源清理链。

业务与 GameFlow **禁止**直接调用 `SceneManager`，统一走 `ISceneService`。

Demo 与手测：`Assets/Test/SceneLayerDemo/`（含 [验证说明.md](../../../Test/SceneLayerDemo/验证说明.md)）。

---

## 1. 快速接入

### 1.1 配置资产放在哪

`SceneCatalog` 是 **ScriptableObject 实例**（`.asset`），与 **C# 类型定义** 分开存放。

| 内容 | 路径 | 说明 |
|------|------|------|
| **类型定义**（代码） | `BaseLayer/SceneLayer/Config/SceneCatalog.cs` | 框架层，**不要**在这里放 `.asset` |
| **正式项目配置**（推荐） | `Assets/HotUpdateScripts/Config/GameSceneCatalog.asset` | 与玩法/热更配置同级，见 ConfigTable 的 `HotUpdateScripts` 约定 |
| **或** | `Assets/Settings/Game/GameSceneCatalog.asset` | 全局游戏设置目录（与 URP 等 `Assets/Settings/` 并列，单独 `Game/` 子目录） |
| **Demo / 测试** | `Assets/Test/SceneLayerDemo/Config/SceneLayerDemoCatalog.asset` | 仅测试用，勿与正式配置混用 |

**不要放在**

| 路径 | 原因 |
|------|------|
| `BaseLayer/`、`BaseFramework/` | 框架目录只放代码，不放项目 SO |
| `Assets/AssetBundle/` | 该目录给 **打进 AB 的资源**；`SceneCatalog` 是编辑器/运行时引导配置，默认**不**打包进 AB |
| 与 `catalog.bytes` 混淆 | `catalog.bytes` 是 **BaseAssetSys 资源清单**（Prefab/Scene 等资源 → bundle），不是 `SceneCatalog` |

**Bootstrap 如何引用**

| 方式 | 适用 |
|------|------|
| `[SerializeField] SceneCatalog catalog` 拖到 Bootstrap / 启动场景 Mono | **推荐**；Init 场景或 DontDestroyOnLoad 上挂引用 |
| `Resources.Load<SceneCatalog>("GameSceneCatalog")` | 资产放在 `Assets/Resources/GameSceneCatalog.asset`（路径与文件名固定，便于无 Inspector 引用时加载） |
| 代码 `ScriptableObject.CreateInstance` + `ReplaceEntries` | 仅 Demo/单元测试（见 `SceneDemoCatalogFactory`） |

正式项目建议 **一种 Catalog 资产 + Bootstrap SerializeField**；多份 Catalog（如 Demo 与正式）分别放在对应目录，由各自 Bootstrap 注入。

### 1.2 创建与编辑

1. Project 窗口右键 → **Create → vFramework → Scene Catalog**
2. 按 §1.1 保存到例如 `Assets/HotUpdateScripts/Config/GameSceneCatalog.asset`
3. Inspector 中为每条场景添加 `SceneEntry`（字段见 §2）

### 1.3 Bootstrap 注册

```csharp
using BaseLayer.Scene;
using BaseLayer.Scene.Impt;

public void Configure(IServiceRegistry services, IModuleRegistry modules)
{
    // 推荐：Bootstrap 场景上 SerializeField 引用 GameSceneCatalog.asset
    // 或 Resources.Load<SceneCatalog>("GameSceneCatalog")
    modules.AddModule(new SceneModule(catalog));
}
```

`SceneModule` Priority = **140**（ConfigTable 之后、GameFlow 之前）。

### 1.4 业务调用

```csharp
_scene = services.Get<ISceneService>();

await _scene.LoadSingleAsync("MainMenu");
await _scene.LoadSingleAsync("Battle_01");
await _scene.LoadAdditiveAsync("Battle_UI", setActive: false);
await _scene.UnloadAsync("Battle_UI");
```

建议用常量类避免硬编码字符串（项目内自建，或参考 Demo 的 `SceneIds`）：

```csharp
public static class SceneIds
{
    public const string MainMenu = "MainMenu";
    public const string Battle = "Battle_01";
}
```

---

## 2. SceneCatalog 配置详解

`SceneCatalog` 是 **逻辑 Id → 加载参数** 的唯一目录。  
`ISceneService` **只认 `SceneEntry.Id`**，不认 Unity 工程路径，也不自动扫描 Build Settings。

### 2.1 字段说明

| 字段 | 必填 | 说明 |
|------|------|------|
| **Id** | 是 | 逻辑名，业务调用 `LoadSingleAsync(Id)` 时使用；全局唯一 |
| **Source** | 是 | `BuildIn`：走 Build Settings；`AssetBundle`：走 catalogue loadPath |
| **UnitySceneName** | BuildIn 必填 | 与 **Build Settings 里勾选的场景名**一致（文件名无 `.unity`） |
| **SceneLoadPath** | AB 必填 | 清单简路径，相对 `Assets/AssetBundle/`，无扩展名，如 `Scenes/SceneDemo_Game` |
| **DefaultMode** | 否 | 默认 `Single`；仅作配置语义，实际以 API（`LoadSingle` / `LoadAdditive`）为准 |
| **Cleanup** | 否 | Single 离开时的清理策略，默认 `FullUnloadAll` |
| **PreloadBundles** | 否 | 场景**进入后**额外 `PreLoadBundles`；见 §2.5（AB 场景通常可留空） |
| **OwnedBundles** | AB 建议填 | 卸载时 `UnloadPackageBundles`；与 Preload **职责不同**，见 §2.5 |
| **SetActiveOnLoad** | 否 | 加载完成后是否 `SetActiveScene`；Additive 子场景可设 `false` |

`UnitySceneName` 为空时，会回退为 `SceneLoadPath` 最后一段或 `Id`（见 `SceneEntry.ResolveUnitySceneName()`）。

### 2.2 Build-in 场景（Build Settings）

**条件**

1. 场景在 **File → Build Settings** 中勾选
2. `SceneCatalog` 中 `Source = BuildIn`，且 `UnitySceneName` 与 Build Settings 中的名字一致

**示例**

| 工程路径 | Build Settings 名 | SceneEntry |
|----------|-------------------|------------|
| `Assets/Scenes/MainMenu.unity` | `MainMenu` | `Id=MainMenu`, `Source=BuildIn`, `UnitySceneName=MainMenu` |

```csharp
new SceneEntry
{
    Id = "MainMenu",
    Source = SceneSource.BuildIn,
    UnitySceneName = "MainMenu",
    Cleanup = SceneCleanupPolicy.FullUnloadAll,
    PreloadBundles = new[] { "common.bundle", "ui.bundle" },
    SetActiveOnLoad = true
}
```

**注意**

- 只在 Build Settings 注册、**未**写入 `SceneCatalog` → Unity 原生可 `LoadScene`，但 **`ISceneService` 会报错**（`SceneId not in catalog`）
- 没有「按 Unity 场景名直接加载」的 API；必须先配 Catalog 再调 `LoadSingleAsync(Id)`

### 2.3 AssetBundle 场景

**条件**

1. `.unity` 已打进 AB（Default 规则：`Assets/AssetBundle/Scenes/` → `scenes/scenes.bundle`；或 Custom 规则单独一包）
2. 构建后 catalogue 含 entry：`loadPath = Scenes/YourScene`
3. **可不**加入 Build Settings（Demo 的 `SceneDemo_Game` 即如此）

**示例**

```csharp
new SceneEntry
{
    Id = "Battle_01",
    Source = SceneSource.AssetBundle,
    UnitySceneName = "Battle_01",           // AB 内 scene 名，通常与文件名一致
    SceneLoadPath = "Scenes/Battle_01",     // catalogue loadPath
    OwnedBundles = new[] { "scenes/scenes.bundle" },
    // PreloadBundles 通常留空：AB 场景加载时已 Acquire 场景包 + 依赖链（§2.5）
    SetActiveOnLoad = true
}
```

打包与验收见 `Assets/Test/SceneLayerDemo/验证说明.md` § Catalogue 验收（P-075）。

### 2.4 Additive 叠加场景

用于 UI 层、小地图等不替换主场景的内容：

```csharp
new SceneEntry
{
    Id = "BattleOverlay",
    Source = SceneSource.BuildIn,
    UnitySceneName = "BattleOverlay",
    DefaultMode = SceneLoadMode.Additive,
    Cleanup = SceneCleanupPolicy.SceneLocalOnly,  // 不 UnloadAll
    SetActiveOnLoad = false                       // 保持主场景 Active（对象池按 Active Scene 分池）
}
```

```csharp
await _scene.LoadAdditiveAsync("BattleOverlay", setActive: false);
// ...
await _scene.UnloadAsync("BattleOverlay");
```

### 2.5 PreloadBundles 与自动拆包 / 依赖加载

打包侧的 **SharedBundlePlanner**（如 `shared_auto.bundle`）与运行时的 **`AcquireBundleWithDependencies`**，解决的是「依赖写进 manifest / catalogue 后，拉主包时**自动带上依赖链**」。  
`SceneEntry.PreloadBundles` 是场景调度层的 **策略字段**：进场景后额外调用 `BundleResLoader.PreLoadBundles`，**不是**依赖解析器。

三者分工：

| 机制 | 阶段 | 作用 |
|------|------|------|
| 自动公共包（SharedBundlePlanner） | 打包 | 跨包引用抽到公共包，写入 `bundles[].dependencies` |
| `AcquireBundleWithDependencies` | 运行时 Load / 进 AB 场景 | 拉主包时**按 catalogue 依赖顺序自动 Acquire**，含 shared |
| `PreloadBundles`（SceneCatalog） | 场景**进入后** | 额外钉住指定包名（`preloadedBundleRefs`，至 `UnloadAll`） |

**会漏包吗？**

- **依赖链上的包不会漏**：AB 场景由 `SceneBundleLoader` 在 Loading 阶段 `AcquireBundleWithDependencies(场景包)`，**不依赖** `PreloadBundles` 是否配置。
- **会漏的情况**：某 AB 包**不在场景包依赖链上**，且进场景后既没有配 `PreloadBundles`、业务也还没 `Load<T>`——这是打包拓扑或业务引用问题，Preload 只是手动补名单，不会自动扫全工程。

**对场景调度层还有用吗？**

| 场景来源 | 调度层已做 | `PreloadBundles` 建议 |
|----------|------------|------------------------|
| **AssetBundle 场景** | 场景包 + catalogue **全部依赖** 已 Acquire | **通常留空**；再填场景包或其依赖多为重复钉引用 |
| **Build-in 场景** | `SceneManager.LoadScene` **不会**拉任何 AB | **建议按需填写**进场景就要用的包，如 `common.bundle`、`ui.bundle` |
| **链外并行包** | 不会自动拉 | **需要填写**或进场景后由业务 `Load<T>` 触发 |

**与 `OwnedBundles` 的区别**

| 字段 | 职责 |
|------|------|
| `OwnedBundles` | **卸载**时 `UnloadPackageBundles` 用，标记场景关联哪些包 |
| `PreloadBundles` | **进入后预热/钉住**引用，偏 Build-in 或链外资源；AB 场景依赖链已由 `SceneBundleLoader` 覆盖 |

**实践建议**

1. AB 场景：`PreloadBundles` **留空**即可（Demo `SceneDemo_Game` 即如此）。  
2. Build-in 主菜单 / Hub：填写进场景立刻要用的 AB。  
3. **不要**重复填写已在 manifest 依赖里的包（如 `shared_auto.bundle`）。  
4. Boot 阶段若已统一 `PreLoadBundles`，SceneEntry 上可再简化，避免重复预热。

资源侧 Preload API 详见 [BusinessApiUsageGuide.md](../BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md) §1。

---

## 3. 配置与运行时对照

```text
业务调用 LoadSingleAsync("MainMenu")
    → SceneCatalog.TryGetEntry("MainMenu")
    → Source == BuildIn ?
          SceneManager.LoadSceneAsync(UnitySceneName, Single)
      : Source == AssetBundle ?
          SceneBundleLoader.LoadSceneFromBundleAsync(SceneLoadPath, ...)
    → SetActiveScene（若 SetActiveOnLoad）
    → PreloadBundles（若配置）
    → SceneTransitionEvent(Completed)
```

Single 切换前（`Cleanup = FullUnloadAll`）自动执行：

1. `SceneBundleLoader.ReleaseAll`
2. `BundleResLoader.UnloadAll`
3. 销毁 `PoolRuntime` + `GameEventBus.ClearAll`

---

## 4. 铁律

| 规则 | 说明 |
|------|------|
| 禁止直接 `SceneManager` | 切场景走 `ISceneService` |
| Catalog 必配 | Build Settings  alone 不够，每条可加载场景需 `SceneEntry` |
| Single 前清理 | `UnloadAll` + 池根 + 事件总线 |
| Additive 不 UnloadAll | 卸载用 `UnloadAsync` |
| 旧句柄失效 | Single 切换后勿再用旧 `IAssetHandle` / 池 |

资源侧详见 [BusinessApiUsageGuide.md](../BaseFramework/BaseAssetSys/Docs/BusinessApiUsageGuide.md) §5.4–5.5。

---

## 5. 事件与 GameFlow

**Loading UI**：订阅 `SceneTransitionEvent`，读 `Phase` / `Progress`（`SceneTransitionPhase` 枚举）。

**GameFlow**：宏观阶段用 `IGameFlowService`；在 `IGameFlowState.Enter` 内 `await ISceneService.LoadSingleAsync(...)`。见 [GameFlowApi.md](../BaseFramework/BaseGameRoot/GameFlow/GameFlowApi.md)。

**并发策略**：`ISceneService.ConflictPolicy` 默认 `ReplacePending`（新请求替换 pending）；可改为 `Queue` 排队。

---

## 6. 目录

```text
BaseLayer/SceneLayer/
├── Config/       SceneCatalog.cs、SceneIds.cs（仅 C#；.asset 实例见 §1.1）
├── Interfaces/
├── Events/
└── Impt/

项目资产示例：
  Assets/HotUpdateScripts/Config/GameSceneCatalog.asset   ← 正式
  Assets/Test/SceneLayerDemo/Config/*.asset               ← Demo
```

AB 场景底层：`BaseAssetSys/ResLoader/Business/SceneBundleLoader.cs`。
