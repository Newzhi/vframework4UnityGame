# Unity 游戏资源管理系统 — 通用设计指南（AI / 开发者参考）

> **文档类型：** 架构设计参考（与具体项目无关）  
> **读者：** 需要设计或实现资源管理模块的开发者、代码生成 AI  
> **用法：** 实现前先读「必做清单」；实现时对照伪代码骨架；交付前跑「检查表」

---

## 0. 给 AI 的阅读说明

若你（AI）被要求「设计/实现一个 Unity 资源管理系统」，请按以下顺序理解本文：

1. **第 1 节**：系统边界与核心目标 — 知道「管什么、不管什么」  
2. **第 2 节**：必做 / 建议做 / 可选 — 知道**绝对不能少**什么  
3. **第 3 节**：模块划分与数据结构的伪代码 — 知道**怎么拆类**  
4. **第 4~6 节**：加载、卸载、Prefab 的完整流程伪代码 — 知道**怎么写主逻辑**  
5. **第 7~9 节**：AB、异步、路径等专题  
6. **第 10 节**：MVP 最小骨架 — 可直接据此生成第一版代码  
7. **第 11 节**：检查表 — 交付前验证  

**禁止事项（全局约束）：**

- 业务层不得直接调用 `Resources.Load`、`AssetBundle.LoadFromFile` 等底层 API（Editor 工具除外）
- 不得在没有引用计数的情况下卸载共享 Asset
- 不得假设 `Destroy(gameObject)` 会自动释放 AssetBundle

---

## 1. 系统要解决什么问题

### 1.1 核心目标（一句话）

> **业务只表达「要什么资源、用完了」；系统负责「从哪加载、缓存、引用计数、何时释放」。**

### 1.2 系统管什么 / 不管什么

| 管 | 不管 |
|----|------|
| Asset / AssetBundle 的加载与卸载 | 游戏逻辑（伤害、AI、网络） |
| 同一资源的缓存与共享 | 对象池内部的 Transform 复位（可协作） |
| 引用计数与释放时机 | UI 布局、动画播放 |
| 异步加载与请求合并 | 场景逻辑编排 |
| 逻辑路径 → 物理路径的映射 | 打包流水线（可对接，但属 Editor 范畴） |

### 1.3 典型痛点（设计动机）

```
痛点 A：同一 icon 被 List 里 20 个 Item 各 Load 一次 → 需要「缓存 + 异步合并」
痛点 B：关 UI 后内存不降 → 需要「引用计数 + 正确 Unload」
痛点 C：Destroy 了 GameObject，AB 还在 → 需要「实例 → Resource 映射表」
痛点 D：Editor 正常、真机 Shader 粉 → 需要「AB 后处理 / Shader 修复钩子」
痛点 E：UI 频繁开关卡顿 → 需要「延迟卸载」或「预加载」
```

---

## 2. 必做 / 建议做 / 可选

### 2.1 必做（MUST）— 缺了就不是合格的资源系统

| # | 能力 | 原因 |
|---|------|------|
| M1 | **单一门面入口**（如 `ResourceManager`） | 统一 API、可替换后端 |
| M2 | **路径 → Resource 缓存字典** | 避免重复加载 |
| M3 | **引用计数**（Load +1 / Unload -1，为 0 才释放） | 共享资源安全释放 |
| M4 | **抽象 Resource + 统一生命周期**（Load/Unload 模板方法） | 多后端行为一致 |
| M5 | **Prefab：模板 Asset 与 Instance 分离管理** | Unity Prefab 有两层生命周期 |
| M6 | **InstanceID → Resource 映射 + DestroyInstance API** | 防止 Destroy 后泄漏 |
| M7 | **异步加载的请求合并**（同 path 只 IO 一次） | 性能与正确性 |
| M8 | **加载完成时 ref==0 则立即丢弃** | 避免无效加载占内存 |

### 2.2 强烈建议（SHOULD）— 商业项目几乎都需要

| # | 能力 | 原因 |
|---|------|------|
| S1 | **工厂模式**按运行环境创建不同 Resource 实现 | Editor / AB / 远程 分支干净 |
| S2 | **延迟卸载队列**（如 15~30 秒） | 减轻 UI 反复开关的 IO 抖动 |
| S3 | **Resource 状态机**（Unloaded/Loading/Loaded/Failed） | 避免重复 Load、异步竞态 |
| S4 | **逻辑路径规范**（业务不写 Assets/ 前缀与扩展名） | 降低耦合 |
| S5 | **AB 两阶段卸载**（Unload(false) 释文件 + UnloadAsset 释对象） | Unity AB 标准实践 |
| S6 | **依赖 Bundle 递归加载** | AB 有依赖时否则 Load 失败 |
| S7 | **调试日志钩子**（Load/Unload/RefCount） | 泄漏排查 |

### 2.3 可选（MAY）— 按项目阶段加

| # | 能力 |
|---|------|
| O1 | Resource 对象池（工厂复用 Resource 实例，减 GC） |
| O2 | GameObject 对象池（与资源系统分层协作） |
| O3 | 远程资源 / Web 下载后端 |
| O4 | 分包下载、热更新、版本校验 |
| O5 | 资源优先级、内存预算、强制 GC |
| O6 | Editor 可视化面板（已加载列表、引用数） |
| O7 | UniTask / async-await 封装 |
| O8 | Shader 丢失自动修复 |

---

## 3. 模块划分与核心数据结构

### 3.1 分层架构（思路）

```
┌─────────────────────────────────────────┐
│  Business（UI、战斗、地图…）              │
│  只调用 ResourceManager 的公开 API         │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  ResourceManager（门面 / Facade）         │
│  - 缓存字典、引用调度、异步合并、实例映射   │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│  Resource（抽象）+ ResourceFactory（工厂）  │
│  模板方法：Load / Unload / AddRef         │
└──────────────────┬──────────────────────┘
                   │
     ┌─────────────┼─────────────┐
     ▼             ▼             ▼
 LocalRes      BundleRes      WebRes …
 (Resources)   (AssetBundle)  (HTTP)
```

### 3.2 核心枚举与类型（伪代码）

```pseudo
enum ResourceState {
    Unloaded,    // 未加载
    Loading,     // 加载中（含异步）
    Loaded,      // 已加载可用
    Failed       // 加载失败
}

enum StorageType {
    Auto,        // 由系统根据平台决定
    Local,       // Resources / AssetDatabase 直连
    Bundle,      // AssetBundle
    Web          // 远程
}

enum ResourceKind {
    Prefab, Material, Texture, Text, Audio, Shader, Other
}

class Resource {
    string logicalPath          // 逻辑路径，如 "UI/Shop/Panel"
    ResourceKind kind
    StorageType storage
    ResourceState state
    int refCount
    Object asset                // 加载结果（Prefab 模板、Texture 等）
    float pendingUnloadTimer    // 延迟卸载计时（可选）

    // --- 模板方法（对外 internal，子类实现钩子）---
    function Load()             // 同步
    function LoadAsync(onDone)  // 异步
    function Unload()           // 真正释放 asset

    // 钩子（子类 override）
    function OnLoad()
    function OnLoadAsync(onDone)
    function OnUnload()
    function OnAddRef()
    function OnRemoveRef()
}

class ResourceManager {
    // 已加载或加载中的资源
    map<string, Resource> activeResources

    // 引用为 0、等待延迟卸载
    map<string, Resource> pendingUnload

    // 异步：path → 进行中的请求（含 callback 列表）
    map<string, AsyncRequest> inFlightRequests

    // Prefab 实例 → 来源 Resource（key = instance.GetInstanceID()）
    map<int, Resource> instanceToResource

    // 工厂注册表
    map<StorageType, ResourceFactory> factories

    const float DELAY_UNLOAD_SEC = 15.0
}
```

### 3.3 工厂（伪代码）

```pseudo
abstract class ResourceFactory {
    function Create(path, kind) -> Resource
    function Recycle(resource)   // 可选：放回对象池
}

class BundleResourceFactory extends ResourceFactory {
    function Create(path, kind) -> BundleResource
}

class LocalResourceFactory extends ResourceFactory {
    function Create(path, kind) -> LocalResource
}

// 初始化时注册
function ResourceManager.init() {
    if (isEditor && !forceBundleMode) {
        factories[Local] = new LocalResourceFactory()
    } else {
        factories[Bundle] = new BundleResourceFactory()
    }
    factories[Web] = new WebResourceFactory()
}

function ResolveStorageType(requested: StorageType) -> StorageType {
    if (requested != Auto) return requested
    if (isBundleMode) return Bundle
    return Local
}
```

**思路：** 业务永远传 `Auto`；平台差异在门面内部一次决定，不散落在业务 if-else 里。

---

## 4. 同步加载 — 完整思路与伪代码

### 4.1 思路（逐步）

```
Step 1: 规范化路径（加前缀、去扩展名）
Step 2: 查 activeResources → 有则 ref++，若已 Loaded 直接返回
Step 3: 查 pendingUnload → 有则取消延迟卸载，移回 active，ref++
Step 4: 都没有 → 工厂 Create → ref++ → 同步 OnLoad → 放入 activeResources
Step 5: 返回 Resource（或 Resource.asset）
```

### 4.2 伪代码

```pseudo
function Load(logicalPath, kind, storage = Auto) -> Resource {
    path = NormalizePath(logicalPath, kind)

    res = activeResources[path]
    if (res != null) {
        res.AddRef()
        if (res.state == Loaded) return res
        // 若在 Loading，同步 Load 应阻塞或报错；通常同步 API 只用于已缓存或小资源
    }

    res = pendingUnload[path]
    if (res != null) {
        pendingUnload.remove(path)
        activeResources[path] = res
        res.AddRef()
        return res
    }

    storage = ResolveStorageType(storage)
    factory = factories[storage]
    res = factory.Create(path, kind)
    res.AddRef()
    activeResources[path] = res

    res.Load()   // 内部：state=Loading → OnLoad → state=Loaded|Failed
    return res
}

function NormalizePath(path, kind) {
    // 例：Prefab 自动加 "Prefabs/" 前缀；去掉 .prefab 后缀由后端解析
    if (kind == Prefab && !path.startsWith("Prefabs/"))
        path = "Prefabs/" + path
    return path
}
```

---

## 5. 异步加载 — 完整思路与伪代码

### 5.1 思路

```
Step 1: 已 Loaded → 直接 callback（可同帧调用）
Step 2: 已在 inFlight → 把 callback 追加到列表，不发起新 IO
Step 3: 否则 Create Resource，ref++，发起 OnLoadAsync，登记 inFlight
Step 4: 完成时：触发所有 callback → 清 inFlight
Step 5: 若此时 refCount==0 → 立刻 Unload（没人要了，白加载）
```

### 5.2 伪代码

```pseudo
class AsyncRequest {
    Resource resource
    list<Callback> callbacks
}

function LoadAsync(logicalPath, kind, callback, storage = Auto) {
    path = NormalizePath(logicalPath, kind)

    res = activeResources[path]
    if (res != null && res.state == Loaded) {
        res.AddRef()
        callback(res)
        return
    }

    req = inFlightRequests[path]
    if (req != null) {
        req.resource.AddRef()
        req.callbacks.add(callback)
        return
    }

    storage = ResolveStorageType(storage)
    res = factories[storage].Create(path, kind)
    res.AddRef()
    activeResources[path] = res

    req = new AsyncRequest(res)
    req.callbacks.add(callback)
    inFlightRequests[path] = req

    res.LoadAsync(onInternalComplete)
}

function onInternalComplete(Resource res) {
    path = res.logicalPath
    req = inFlightRequests[path]
    if (req == null) {
        // 加载期间已被完全 Unload，请求已取消
        if (res.refCount == 0) res.Unload()
        return
    }

    inFlightRequests.remove(path)

    for (cb in req.callbacks) {
        cb(res)
    }

    if (res.refCount == 0) {
        // 关键：异步完成时已无人持有 → 不要留着
        DoUnload(path, res)
    }
}
```

**AI 注意：** `inFlight` 与 `refCount` 必须配合。合并请求时每个 caller 都应 `AddRef()`，callback 里若不再使用需 `Unload()`。

---

## 6. Prefab：实例化与销毁 — 完整思路与伪代码

### 6.1 两层概念（必须理解）

```
┌──────────────────────────────────────┐
│  Prefab Template (Asset)             │  ← Resource.asset，存在资源系统缓存里
│  Load 加载的是这一层                   │
└──────────────────┬───────────────────┘
                   │ Instantiate()
                   ▼
┌──────────────────────────────────────┐
│  GameObject Instance（场景里的对象）    │  ← 业务可见；Destroy 只毁这一层
└──────────────────────────────────────┘
```

- **LoadPrefab** = Load 模板 + Instantiate + 记录映射  
- **DestroyInstance** = Destroy 实例 + 对模板 Unload（减 ref）

### 6.2 伪代码

```pseudo
function LoadPrefab(logicalPath, prewarm = false) -> GameObject {
    res = Load(logicalPath, Prefab)

    template = res.asset as GameObject
    if (template == null) return null

    if (prewarm) {
        // 只预热模板，不实例化（可选优化）
        return template
    }

    instance = Instantiate(template)
    instanceToResource[instance.instanceId] = res
    // 注意：Instantiate 不增加 Resource ref；Load 已 +1。
    // 实例生命周期结束时应 DestroyInstance → Unload 减 ref。
    return instance
}

function InstantiateFrom(Resource res) -> GameObject {
    instance = Instantiate(res.asset)
    instanceToResource[instance.instanceId] = res
    return instance
}

function DestroyInstance(Object obj) {
    if (obj == null) return

    id = obj.instanceId
    res = instanceToResource[id]

    if (res != null) {
        instanceToResource.remove(id)
        // Prefab 实例：Destroy；模板 ref 由 Unload 减
        if (obj is GameObject && res.kind == Prefab) {
            Destroy(obj)
        }
        Unload(res)   // ref--
    } else {
        // 非资源系统创建的对象，仅 Destroy
        Destroy(obj)
    }
}

function Unload(Resource res) {
    path = res.logicalPath
    res.RemoveRef()   // ref--

    if (res.refCount > 0) return

    // ref == 0：从 active 移除，进入延迟卸载或立即卸载
    activeResources.remove(path)
    inFlightRequests.remove(path)   // 若还在加载，应取消或完成后丢弃

    if (USE_DELAY_UNLOAD) {
        res.pendingUnloadTimer = 0
        pendingUnload[path] = res
    } else {
        res.Unload()
        factories[res.storage].Recycle(res)
    }
}

function Update(deltaTime) {
    // 延迟卸载 tick
    for (path, res in pendingUnload) {
        res.pendingUnloadTimer += deltaTime
        if (res.pendingUnloadTimer >= DELAY_UNLOAD_SEC) {
            pendingUnload.remove(path)
            res.Unload()
            factories[res.storage].Recycle(res)
        }
    }
}
```

### 6.3 再次 Load 时取消延迟卸载

```pseudo
function AcquireFromCacheOrPending(path) -> Resource? {
    if (activeResources.has(path)) return activeResources[path]
    if (pendingUnload.has(path)) {
        res = pendingUnload.remove(path)
        activeResources[path] = res
        return res
    }
    return null
}
```

---

## 7. 抽象 Resource 子类 — 模板方法伪代码

### 7.1 基类

```pseudo
class Resource {
    function Load() {
        if (state == Loaded || state == Failed) return
        state = Loading
        OnAddRef()      // 可选：与底层同步
        OnLoad()
        state = (asset != null) ? Loaded : Failed
    }

    function LoadAsync(callback) {
        if (state == Loaded) { callback(this); return }
        if (state == Failed) { callback(this); return }
        state = Loading
        OnLoadAsync(function(success) {
            state = success ? Loaded : Failed
            callback(this)
        })
    }

    function Unload() {
        if (state == Unloaded) return
        OnUnload()
        OnRemoveRef()
        asset = null
        state = Unloaded
    }

    function AddRef()    { refCount++; OnAddRef() }
    function RemoveRef() { refCount--; OnRemoveRef() }
}
```

### 7.2 Bundle 后端示例

```pseudo
class BundleResource extends Resource {
    string bundleName
    string assetNameInBundle

    function OnLoad() {
        bundle = BundleManager.LoadBundle(bundleName)   // 含依赖递归
        asset = bundle.LoadAsset(assetNameInBundle, kind)
        bundle.Unload(false)   // ★ 释 AB 文件，保留 asset
        FixShaderIfNeeded(asset)
    }

    function OnUnload() {
        if (asset == null) return
        if (kind == Prefab) {
            // 模板对象：若已无实例，可 DestroyImmediate(template, allowDestroyingAssets=true)
        } else {
            Resources.UnloadAsset(asset)
        }
        asset = null
        BundleManager.ReleaseBundle(bundleName)   // bundle 引用 -1
    }

    function OnAddRef()    { BundleManager.RetainBundle(bundleName) }
    function OnRemoveRef() { BundleManager.ReleaseBundle(bundleName) }
}
```

### 7.3 Local 后端示例

```pseudo
class LocalResource extends Resource {
    function OnLoad() {
        asset = Resources.Load(logicalPath)
    }

    function OnUnload() {
        if (kind != Prefab && asset != null)
            Resources.UnloadAsset(asset)
        asset = null
    }
}
```

---

## 8. BundleManager — 依赖与 AB 卸载（伪代码）

### 8.1 加载依赖（必做，若使用 AB）

```pseudo
class BundleManager {
    map<string, BundleHandle> loadedBundles   // bundleName → handle
    map<string, string[]> dependencyGraph       // 打包时生成的 manifest

    function LoadBundle(name) -> BundleHandle {
        if (loadedBundles.has(name)) {
            loadedBundles[name].retainCount++
            return loadedBundles[name]
        }

        // 先递归加载依赖
        for (dep in dependencyGraph[name]) {
            LoadBundle(dep)
        }

        filePath = ResolveBundlePath(name)
        ab = AssetBundle.LoadFromFile(filePath)
        handle = new BundleHandle(name, ab)
        handle.retainCount = 1
        loadedBundles[name] = handle
        return handle
    }

    function ReleaseBundle(name) {
        handle = loadedBundles[name]
        if (handle == null) return
        handle.retainCount--
        if (handle.retainCount > 0) return

        handle.bundle.Unload(true)   // 或先 Unload(false) 若 asset 已单独管理
        loadedBundles.remove(name)
    }
}
```

### 8.2 AB 卸载参数决策（参考表）

| 时机 | 调用 | 含义 |
|------|------|------|
| LoadAsset 成功后 | `bundle.Unload(false)` | 释放 AB 容器内存，**保留**已 Load 的 Asset |
| 确认 Asset 已 UnloadAsset | `bundle.Unload(true)` 或不再持有 bundle | 彻底释放 |
| 整包切换 / 热更后 | 全局 `UnloadAll(false)` 或 `UnloadAll(true)` | 按是否保留已实例化对象选择 |

**AI 实现建议：** 默认采用「Load 后 `Unload(false)` + Asset 引用归零时 `UnloadAsset`」两阶段策略。

---

## 9. 路径、API 设计参考

### 9.1 逻辑路径规则（建议）

```pseudo
// 业务侧只写：
"UI/Shop/ShopPanel"
"Characters/Hero01"
"Config/Level_01"

// 资源系统内部解析为：
// Local:  Resources 下的相对路径
// Bundle: "Assets/GameRes/Prefabs/UI/Shop/ShopPanel.prefab"（由打包映射表转换）
```

### 9.2 推荐公开 API 一览

```pseudo
// --- 同步 ---
Load(path, kind) -> Resource
LoadObject<T>(path, kind) -> T
LoadPrefab(path) -> GameObject          // Load + Instantiate + 映射

// --- 异步 ---
LoadAsync(path, kind, callback)
LoadPrefabAsync(path, callback)

// --- 释放 ---
Unload(path)                             // 按路径减 ref
Unload(Resource res)
DestroyInstance(GameObject go)           // Destroy + 减 ref

// --- 工具 ---
Exists(path) -> bool
GetRefCount(path) -> int                 // 调试
```

---

## 10. MVP 最小实现骨架（可直接生成代码）

以下是一版**可运行最小系统**的类清单与职责，AI 可按此生成第一版：

```pseudo
// 文件 1: Resource.cs          — 抽象基类 + 状态机 + 模板方法
// 文件 2: LocalResource.cs     — Resources.Load 实现
// 文件 3: BundleResource.cs    — AB Load + Unload(false) 实现（可第二期）
// 文件 4: ResourceFactory.cs   — 工厂基类 + 具体工厂
// 文件 5: ResourceManager.cs   — 门面：缓存、ref、异步合并、实例映射、Update 延迟卸载
// 文件 6: GameMain / Launcher  — 每帧 ResourceManager.Update()

// ResourceManager 最小字段：
// - activeResources: map<path, Resource>
// - pendingUnload: map<path, Resource>
// - inFlightRequests: map<path, AsyncRequest>
// - instanceToResource: map<instanceId, Resource>

// 最小 API：
// Load, LoadAsync, Unload, LoadPrefab, DestroyInstance, Update
```

### 10.1 MVP 不包含（第二期再加）

- 远程下载、热更新、分包  
- 多工厂切换（第一期可只写 Local 或只写 Bundle）  
- GameObject 对象池  
- Editor 调试面板  

---

## 11. 与对象池的协作（思路）

```pseudo
// 对象池：管 GameObject 复用
class GameObjectPool {
    map<string, queue<GameObject>> pools

    function Get(prefabPath) -> GameObject {
        if (pools[prefabPath].notEmpty())
            return pools[prefabPath].dequeue()
        return ResourceManager.LoadPrefab(prefabPath)   // 首次从资源系统取
    }

    function Return(prefabPath, go) {
        go.SetActive(false)
        pools[prefabPath].enqueue(go)
        // ★ 不 Unload：模板仍被 Resource 持有，ref 不减
    }

    function ClearPool(prefabPath) {
        for (go in pools[prefabPath])
            ResourceManager.DestroyInstance(go)   // 真正释放
        pools.remove(prefabPath)
    }
}
```

**原则：** 池活跃期间不减 Resource 引用；**ClearPool / 场景卸载** 时必须 `DestroyInstance`。

---

## 12. 常见错误与反模式（AI 应避免生成）

```pseudo
// ❌ 反模式 1：业务直接加载
asset = Resources.Load("UI/Panel")   // 绕过引用计数

// ❌ 反模式 2：只 Destroy 不 Unload
Destroy(panelGo)   // 模板仍 ref>0，AB 泄漏

// ❌ 反模式 3：每次 Load 都 new 不查缓存
function BadLoad(path) {
    return Resources.Load(path)   // 无共享、无计数
}

// ❌ 反模式 4：异步无合并
for (item in list)
    StartCoroutine(LoadIcon(item))   // 同 icon 并发 N 次 IO

// ❌ 反模式 5：Load 后立即 Unload（以为 Load 不算持有）
res = Load(path)
Unload(res)   // 若别处还要用，会提前释放

// ✅ 正确：Load 与 Unload 成对，跨模块共享靠 refCount
res = Load(path)        // ref=1
// ... 使用 ...
Unload(res)             // ref=0 → 进入延迟卸载或真正释放
```

---

## 13. 决策树（AI 选型用）

```
需要热更新 / 远程资源？
├─ 是 → AB 或 Addressables 后端 + 版本清单 + 下载器（可选模块）
└─ 否 → Local Resources 或 Editor 直连即可

资源体量与平台？
├─ 移动端大项目 → 必须异步 + 延迟卸载 + AB
└─ 小原型 → MVP Local 同步 Load 够用

是否多人共用同一 Asset？
├─ 是 → 必须引用计数
└─ 否 → 仍建议计数（未来可能共用）

是否大量 Prefab？
├─ 是 → 必须 Instance 映射 + DestroyInstance
└─ 否 → 仍建议统一 API
```

---

## 14. 交付检查表（AI / 人类验收）

### 14.1 功能必过项

- [ ] **M1** 业务仅通过 ResourceManager 加载  
- [ ] **M2** 同 path 第二次 Load 不重复 IO（已 Loaded）  
- [ ] **M3** refCount：N 次 Load + M 次 Unload，仅 M>=N 且 ref=0 时释放  
- [ ] **M4** LoadPrefab + DestroyInstance 后 ref 归零  
- [ ] **M5** 异步同 path 并发只 1 次 IO，callback 全部触发  
- [ ] **M6** 异步完成时 ref=0 不残留 Asset  
- [ ] **M7** AB 模式 LoadAsset 后 bundle.Unload(false)  

### 14.2 建议过项

- [ ] 延迟卸载期间再次 Load 可复用，不重新 IO  
- [ ] Failed 状态不无限重试（或有明确重试策略）  
- [ ] Editor 与真机存储类型分支有文档说明  

---

## 15. 设计思路总结（给 AI 的压缩 prompt）

若需用一段话驱动代码生成，可使用：

> 实现 Unity 资源管理系统：单例 ResourceManager 作为唯一门面；Resource 抽象类含状态机与 Load/Unload 模板方法；ResourceFactory 按 Local/Bundle 创建实现；ResourceManager 维护 path→Resource 缓存、refCount、inFlight 异步合并、instanceId→Resource 映射；提供 Load/LoadAsync/LoadPrefab/DestroyInstance/Unload/Update；Unload 时 ref 归零进 15s 延迟队列；Bundle 后端 Load 后 Unload(false)，释放时 UnloadAsset；禁止业务直接 Resources.Load。

---

## 16. 七条核心原则（人类可读版）

1. **一个入口** — 业务只认 ResourceManager  
2. **一层抽象** — Resource 统一生命周期  
3. **一套工厂** — 平台差异关在后端  
4. **引用计数** — 共享资源安全释放  
5. **实例映射** — Destroy 时能减到正确 Resource  
6. **异步合并** — 同 path 只加载一次  
7. **延迟卸载** — 内存与 IO 的平衡  

---

*本文档为通用设计参考，不包含任何具体项目名称、框架名称或仓库路径。实现时可与项目专用设计文档对照，但对外分享或 AI 训练应使用本文档级别抽象。*
