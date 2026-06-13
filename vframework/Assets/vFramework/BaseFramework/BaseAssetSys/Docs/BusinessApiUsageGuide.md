# 业务 API 调用指南（ABSystem_Beta）

> 入口：`BundleResLoader.Instance`  
> 句柄：`IAssetHandle`  
> 详细排期与能力边界：[BusinessApiAndCdnPlanning.md](./BusinessApiAndCdnPlanning.md)、[MainRoadmap.md](./MainRoadmap.md)

---

## 1. 初始化

| API | 说明 |
|-----|------|
| `BundleResLoader.Instance.EnsureReady()` | 懒加载 Catalogue 与 Bundle 根目录；可在首次 `Load` 前预热 |
| `BundleResLoader.Instance.Init(bundleRootPath, usePlatformSubfolder)` | 显式指定 AB 根路径；重复 Init 会打 Warning |
| `BundleResLoader.GetDefaultRuntimeBundleRoot()` | 默认 `StreamingAssets/{当前平台}/` |
| `BundleResLoader.Instance.GetCatalogue()` | 读取已加载清单（调试 / 查 `buildMode`） |

```csharp
// 一般无需手动 Init；首次 Load 会自动 EnsureReady
if (!BundleResLoader.Instance.EnsureReady())
{
    Debug.LogError("Catalogue init failed.");
    return;
}
```

---

## 2. 同步加载

### 2.1 `Load<T>(loadPath)` — 业务主入口

- `loadPath`：清单 **简路径**，无扩展名。  
- `Resources/` 开头：走 Resources 分支，例 `Load<TextAsset>("Resources/ResourceSystemDesignGuide")`。  
- 失败返回 **`null`**，不增加引用计数。

```csharp
IAssetHandle handle = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot");
if (handle == null) return;

GameObject prefab = handle.GetAsset<GameObject>();   // 仅取原型，不实例化
GameObject go = handle.Instantiate();                // 实例化
```

### 2.2 `LoadByAssetPath<T>(assetPath)` — 按工程完整路径

```csharp
IAssetHandle handle = BundleResLoader.Instance.LoadByAssetPath<Sprite>(
    "Assets/AssetBundle/Atlas/Role/Hog.png");
```

### 2.3 `LoadByBundle<T>(bundleName, assetName, assetPath?, loadPath?)` — 按包名

```csharp
IAssetHandle handle = BundleResLoader.Instance.LoadByBundle<Sprite>(
    "atlas.bundle",
    "Hog_Attack_000");
```

---

## 3. 异步加载

当前异步 API 内部为 **`UniTask.Yield` 一帧 + 同步 `Load`**；须 `await` 或回调后再使用句柄。

### 3.1 `LoadUniTaskAsync<T>(loadPath)`

```csharp
IAssetHandle handle = await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>("UI/UIRoot");
GameObject go = handle?.Instantiate();
```

### 3.2 `LoadUniTaskWithCallback<T>(loadPath, onComplete, onFailed, useUniTask)`

```csharp
BundleResLoader.Instance.LoadUniTaskWithCallback<GameObject>(
    "UI/UIRoot",
    onComplete: handle =>
    {
        if (handle == null) return;
        GameObject go = handle.Instantiate();
    },
    onFailed: err => Debug.LogError(err));
```

### 3.3 按路径 / 按 Bundle 的回调重载

```csharp
BundleResLoader.Instance.LoadByAssetPathUniTaskWithCallback<Sprite>(assetPath, onComplete, onFailed);
BundleResLoader.Instance.LoadByBundleUniTaskWithCallback<Sprite>(bundleName, assetName, onComplete, onFailed, assetPath: null);
```

- `useUniTask: false` 时改为同步 `Load` 并立即回调。

---

## 4. 句柄 `IAssetHandle`

| 成员 | 说明 |
|------|------|
| `GetAsset<T>()` | 取已加载资源（Sprite、Material、Prefab 原型等） |
| `Instantiate()` | 在原点实例化 GameObject |
| `InstantiateAt(pos, rot, parent)` | 指定位置 / 父节点实例化 |
| `Instance` | 等价于 **每次访问** 调用 `Instantiate()`；**勿重复读** |
| `Release()` | Ref -1；Ref 为 0 时立即卸载资源占用 |

```csharp
IAssetHandle handle = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");

GameObject a = handle.Instantiate();
GameObject b = handle.InstantiateAt(spawnPos, Quaternion.identity, parent);

Sprite icon = handle.GetAsset<Sprite>();  // 非 GameObject 资源
```

- `Release()` 在 Ref 已为 0 时为 **no-op**（重复调用安全）。  
- 同一路径多次 `Load` 命中缓存时返回 **同一句柄对象**，Ref 累加。

---

## 5. 卸载

### 5.1 `Unload(IAssetHandle resource, GameObject instance, Action<bool> onComplete)`

```csharp
BundleResLoader.Instance.Unload(handle, go);
BundleResLoader.Instance.Unload(handle, null);           // 仅 Release
BundleResLoader.Instance.Unload(null, go);             // 仅 Destroy
BundleResLoader.Instance.Unload(handle, go, ok => { }); // 可选回调
```

- 顺序：**先** `Destroy(instance)`（非 null），**再** `resource.Release()`。  
- 实例已在别处 `Destroy`：用 `handle?.Release()` 或 `Unload(handle, null)`。

### 5.2 `Release()`

```csharp
handle?.Release();
```

### 5.3 `UnloadAll()`

```csharp
BundleResLoader.Instance.UnloadAll();
```

- 进程级收尾（切场景 / 关游戏）；清空全部 Resource 缓存并 `BundleManager.UnloadAll()`。  
- 与单资源 `Release` 分开使用，避免混用。

---

## 6. 引用计数与规范用法

### 6.1 规则速查

| 操作 | Resource Ref |
|------|----------------|
| `Load` 成功 ×1 | +1 |
| `Instantiate` ×N | 不变 |
| `Destroy` 实例 ×N | 不变 |
| `Release` ×1 | -1；为 0 时卸载 |
| 同路径 `Load` ×N（缓存命中） | +N |

- 每次 **成功** 的 `Load` 最终要有 **一次** `Release` 或 `Unload(handle, …)`。  
- `Load` 返回 `null` → **不要** `Release`。  
- 句柄出作用域 **不会** 自动 Release。  
- Ref 为 0 后 AB 会 `Unload(true)`；场上仍有依赖该资源的实例时会丢 Mesh/材质。**有活实例时不要 Release 到 0。**

### 6.2 必须遵守的写法

**① 保存句柄**

```csharp
// ✅ 字段 / 成员保存 IAssetHandle
IAssetHandle _handle;

// ❌ 只留 GameObject，无法 Release
GameObject _go = BundleResLoader.Instance.Load<GameObject>(path)?.Instantiate();
```

**② Load 与 Release 次数成对**

```csharp
// ✅ 一次 Load，一次 Release（模块 OnDestroy）
_handle = BundleResLoader.Instance.Load<GameObject>(path);
void OnDestroy() { _handle?.Release(); _handle = null; }

// ✅ N 次 Load，N 次 Release（每实例各绑一次 Load）
void Spawn() {
    var h = BundleResLoader.Instance.Load<GameObject>(path);
    go.GetComponent<X>().Bind(h);  // OnDestroy 里 h.Release()
}

// ❌ 一次 Load，多个实例各自 Release 同一句柄
_handle = Load(...);
for (...) Spawn();  // 子物体 OnDestroy 里 _handle.Release() → 第一个就 Ref=0
```

**③ 多实例 + 单次 Load：只 Release 一次**

```csharp
// ✅ Load 1 → Instantiate N → 模块 OnDestroy Release 1 次
_handle = BundleResLoader.Instance.Load<GameObject>(path);
for (int i = 0; i < n; i++) _list.Add(_handle.Instantiate());
void OnDestroy() {
    foreach (var go in _list) if (go) Destroy(go);
    _handle?.Release();
}

// ❌ 同一 _handle 在循环或子脚本里 Release 多次
```

**④ Destroy 实例 ≠ Release 资源**

```csharp
// ✅ 模块收尾时 Release
Destroy(go);
_handle?.Release();

// ❌ 只 Destroy，从不 Release → Ref 泄漏
Destroy(go);

// ❌ 只 Release，场上实例还在用 → 材质变粉
_handle?.Release();  // 实例仍在场景
```

**⑤ `Unload` 与 `Release` 不要重复减 Ref**

```csharp
// ✅ 二选一
BundleResLoader.Instance.Unload(_handle, _go);
// 或实例已 Destroy 时：
_handle?.Release();

// ❌ 对同一 Load 既 Unload 又 Release
BundleResLoader.Instance.Unload(_handle, _go);
_handle?.Release();  // Ref 多减一次
```

**⑥ 实例化用法**

```csharp
// ✅ 保存 Instantiate 返回值
_go = _handle.Instantiate();

// ✅ 仅要原型、不实例化
var prefab = _handle.GetAsset<GameObject>();

// ❌ 多次读 Instance（每次都会新建 GO）
var a = _handle.Instance;
var b = _handle.Instance;
```

**⑦ 异步 / 回调里也要保存句柄**

```csharp
// ✅
IAssetHandle _handle;
BundleResLoader.Instance.LoadUniTaskWithCallback<GameObject>(path,
    onComplete: h => { _handle = h; },
    onFailed: _ => { });

void OnDestroy() { _handle?.Release(); }

// ❌ 回调里 Instantiate 后未保存 h
onComplete: h => { Instantiate(); }  // 无法 Release
```

**⑧ 多资源：每个句柄单独 Release**

```csharp
void OnDestroy()
{
    BundleResLoader.Instance.Unload(_icon, null);
    BundleResLoader.Instance.Unload(_prefab, _instance);
    // 几个 Load 成功，就几次 Release（可合并进 Unload）
}
```

**⑨ `UnloadAll` 仅进程级收尾**

```csharp
// ✅ 切场景 / 关游戏
BundleResLoader.Instance.UnloadAll();

// ❌ 与单资源 Release 混用同一批资源的日常卸载逻辑
Load(...);
UnloadAll();  // 其它模块仍持有的句柄已失效
```

### 6.3 按场景选一种 Ref 模型（勿混用）

| 场景 | Load | Release |
|------|------|---------|
| 模块 / UI / 一两个实例 | 模块内各 `Load` 1 次 | 模块 `OnDestroy` 各 `Release` 1 次 |
| 同一 Prefab 多实例（低频） | 1 次 | 模块 `OnDestroy` **1 次** |
| 每实例 Destroy 里收尾 | 每 spawn `Load` 1 次 | 实例 `OnDestroy` 各 `Release` 1 次 |
| Load 1 次 + 提前卸 AB | 1 次 + 活实例计数 | 计数为 0 时 **Release 1 次** |
| 对象池 | 池 Init `Load` 1 次 | 池 Clear / 销毁 **Release 1 次**；Return **不** Release |

选定一种后，**生成方与卸载方一致**：模块持句柄就由模块 Release；每实例 Bind 就由实例 Release。

### 6.4 子脚本自控 Destroy 时

- 子脚本 **只** `Destroy(gameObject)`，**不要** `Release` 模块持有的共享句柄。  
- 若要求子脚本 `OnDestroy` 里 Release：spawn 时必须 **每次** `Load` 并把 **该次** 句柄 `Bind` 到实例（见 §7.5）。  
- 实例先于模块 Destroy 时，模块 `OnDestroy` 仍须 `Release`；GO 已为 null 时用 `Unload(handle, null)` 或 `handle?.Release()`。

---

## 7. 场景范例

### 7.1 模块持句柄 — 字段 + `OnDestroy`（推荐默认）

```csharp
IAssetHandle _prefab;
IAssetHandle _icon;
GameObject _instance;

void Start()
{
    _prefab = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");
    _instance = _prefab?.Instantiate();
    _icon = BundleResLoader.Instance.Load<Sprite>("Icon/3");
}

void OnDestroy()
{
    BundleResLoader.Instance.Unload(_icon, null);
    BundleResLoader.Instance.Unload(_prefab, _instance);
}
```

### 7.2 低频一两个实例

```csharp
IAssetHandle _npcHandle;
GameObject _npc;

void Start()
{
    _npcHandle = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/Npc");
    _npc = _npcHandle?.Instantiate();
}

void OnDestroy()
{
    BundleResLoader.Instance.Unload(_npcHandle, _npc);
}
```

同一 Prefab 两个实例：`Load` 一次，`Instantiate` 两次，`OnDestroy` 里 Destroy 两个 GO 后 **`Release` 一次**。

### 7.3 多个不同 Prefab

```csharp
IAssetHandle _hNpc, _hItem, _hFx, _hUi;
GameObject _npc, _item, _fx, _ui;

void Start()
{
    _hNpc = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/Npc");
    _npc = _hNpc.Instantiate();
    _hItem = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/Item");
    _item = _hItem.Instantiate();
    _hFx = BundleResLoader.Instance.Load<GameObject>("FX/Hit");
    _fx = _hFx.Instantiate();
    _hUi = BundleResLoader.Instance.Load<GameObject>("UI/SubPanel");
    _ui = _hUi.Instantiate();
}

void OnDestroy()
{
    BundleResLoader.Instance.Unload(_hNpc, _npc);
    BundleResLoader.Instance.Unload(_hItem, _item);
    BundleResLoader.Instance.Unload(_hFx, _fx);
    BundleResLoader.Instance.Unload(_hUi, _ui);
}
```

子物体脚本只 `Destroy(gameObject)`，不要对共享句柄 `Release`。

### 7.4 同一 Prefab 多个实例 — 模块统一 Release

```csharp
IAssetHandle _handle;
readonly List<GameObject> _spawned = new();

void Start()
{
    _handle = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/Monster");
    for (int i = 0; i < 4; i++)
        _spawned.Add(_handle.Instantiate());
}

void OnDestroy()
{
    foreach (GameObject go in _spawned)
        if (go != null) Destroy(go);
    _spawned.Clear();
    _handle?.Release();
}
```

多个实例 **不要** 各自 `Release` 同一句柄。

### 7.5 每实例 `OnDestroy` 里 Release

每次生成 `Load` 一次（缓存命中，Ref++），句柄绑到实例上：

```csharp
// 生成方
IAssetHandle handle = BundleResLoader.Instance.Load<GameObject>("FX/Bullet");
GameObject go = handle.Instantiate();
go.GetComponent<Bullet>().Bind(handle);

// 实例脚本
IAssetHandle _handle;
public void Bind(IAssetHandle h) => _handle = h;
void OnDestroy()
{
    _handle?.Release();
    _handle = null;
}
```

### 7.6 Load 一次 + 活实例计数

```csharp
IAssetHandle _handle;
int _live;

void Spawn()
{
    if (_handle == null)
        _handle = BundleResLoader.Instance.Load<GameObject>("FX/Hit");
    _live++;
    var go = _handle.Instantiate();
    go.GetComponent<FxLife>().Bind(this);
}

internal void OnInstanceDestroyed()
{
    if (--_live > 0) return;
    _handle?.Release();
    _handle = null;
}
```

### 7.7 对象池

```csharp
IAssetHandle _handle;
readonly Queue<GameObject> _pool = new();

void InitPool()
{
    _handle = BundleResLoader.Instance.Load<GameObject>("FX/Bullet");
}

GameObject Get()
{
    if (_pool.Count > 0)
    {
        var go = _pool.Dequeue();
        go.SetActive(true);
        return go;
    }
    return _handle.Instantiate();
}

void Return(GameObject go)
{
    go.SetActive(false);
    _pool.Enqueue(go);
}

void ClearPool()
{
    while (_pool.Count > 0)
        Destroy(_pool.Dequeue());
    _handle?.Release();
    _handle = null;
}
```

池活跃期间 **不** `Release`；Clear / 模块销毁时再 Release。

### 7.8 加载 Prefab 并替换贴图

```csharp
IAssetHandle prefab = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");
GameObject go = prefab?.Instantiate();

IAssetHandle icon = BundleResLoader.Instance.Load<Sprite>("Icon/3");
Texture tex = icon?.GetAsset<Sprite>()?.texture;

if (go != null && tex != null)
{
    Material mat = go.GetComponentInChildren<Renderer>().material;
    mat.SetTexture("_BaseMap", tex);
}

// OnDestroy: Unload(icon, null); Unload(prefab, go);
```

### 7.9 跨包依赖

```csharp
IAssetHandle ui = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot");
GameObject uiGo = ui?.Instantiate();
```

依赖包由清单 `bundles[]` 自动加载；须使用含依赖信息的打包清单（EditorTest 占位清单可能无 `bundles[]`）。

---

## 8. 其它注意

- 仅在主线程调用 `Load` / `Unload`。  
- `LoadUniTaskAsync` 当前为 Yield 一帧 + 同步 `Load`；加载失败时 `onFailed` / 判空后再决定是否重试。  
- Editor Play + 清单 `buildMode=EditorTest` 时在 Editor 内走 AssetDatabase；Player 走 AB，见 [ResLoader/README.md](../ResLoader/README.md)。  
- Ref 与范例细则见 **§6**；抄代码见 **§7**。
