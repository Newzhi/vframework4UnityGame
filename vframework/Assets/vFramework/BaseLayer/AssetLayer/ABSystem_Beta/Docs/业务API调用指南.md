# 业务 API 调用指南（ABSystem_Beta）

> 目标：给业务侧一份可直接抄用的加载/卸载范式。  
> 入口类：`BundleResLoader`  
> 句柄接口：`IAssetHandle`

---

## 1. 推荐调用方式（链式）

### 1.1 同步加载 + 链式实例化（推荐）

```csharp
GameObject go = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot")?.Instance;
```

- `?.Instance` 可避免加载失败时空引用。
- `Load<T>(loadPath)` 的 `loadPath` 是清单简路径（无扩展名）。

### 1.2 UniTask 异步加载 + 链式实例化

```csharp
GameObject go = (await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>("UI/UIRoot"))?.Instance;
```

- 方法名含 `UniTask`，表示依赖 Cysharp UniTask 的 `await` 入口。
- 异步必须先 `await` 到 `IAssetHandle`，再链式取 `Instance`。

### 1.3 UniTask 回调加载（默认走 UniTask）

```csharp
BundleResLoader.Instance.LoadUniTaskWithCallback<GameObject>(
    "UI/UIRoot",
    onComplete: handle =>
    {
        GameObject go = handle.Instance;
    },
    onFailed: err =>
    {
        Debug.LogError(err);
    }
);
```

- `useUniTask=false` 时可改为同步 `Load` 并立即回调。

---

## 2. 常见业务场景

### 2.1 加载预制体后替换贴图/材质

```csharp
IAssetHandle prefab = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");
GameObject go = prefab?.Instance;

IAssetHandle icon = BundleResLoader.Instance.Load<Sprite>("Icon/3");
Texture tex = icon?.GetAsset<Sprite>()?.texture;

if (go != null && tex != null)
{
    Material mat = go.GetComponentInChildren<Renderer>().material;
    mat.SetTexture("_BaseMap", tex);
}
```

### 2.2 跨包加载

```csharp
IAssetHandle ui = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot");
GameObject uiGo = ui?.Instance;
```

- 依赖包由清单 `bundles[]` 驱动自动加载。
- 前提：使用包含依赖信息的真实打包清单（非仅 EditorTest 占位产物）。

---

## 3. 卸载资源（尽量一句话）

### 3.1 一句话卸载（句柄 + 实例）

```csharp
BundleResLoader.Instance.Unload(handle, instance);
```

- 会执行：`Destroy(instance)` + `handle.Release()`。

### 3.2 仅卸载句柄

```csharp
handle?.Release();
```

### 3.3 仅销毁实例

```csharp
BundleResLoader.Instance.Unload(null, instance);
```

### 3.4 全量卸载

```csharp
BundleResLoader.Instance.UnloadAll();
```

---

## 4. 推荐生命周期写法

```csharp
IAssetHandle _prefab;
IAssetHandle _icon;
GameObject _instance;

void Start()
{
    _prefab = BundleResLoader.Instance.Load<GameObject>("Model/Prefabs/tester");
    _instance = _prefab?.Instance;
    _icon = BundleResLoader.Instance.Load<Sprite>("Icon/3");
}

void OnDestroy()
{
    BundleResLoader.Instance.Unload(_icon, null);
    BundleResLoader.Instance.Unload(_prefab, _instance);
}
```

---

## 5. 使用注意事项

- 每次 `Load` 成功后都应有对应的 `Release/Unload`，避免引用计数悬挂。
- `Destroy` 与 `Release` 是两件事：销毁实例不等于释放句柄。
- 当前加载器按主线程调用设计；不要在多线程并发直接调用 `Load/Unload`。
- `LoadUniTaskAsync` 当前是“UniTask 异步入口 + 同步加载内核”，后续再接 CDN 下载与并发队列。

