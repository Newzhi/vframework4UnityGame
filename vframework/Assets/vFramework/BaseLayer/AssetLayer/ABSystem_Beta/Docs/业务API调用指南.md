# 业务 API 调用指南（ABSystem_Beta）

> 目标：给业务侧一份可直接抄用的加载/卸载范式。  
> 入口类：`BundleResLoader`  
> 句柄接口：`IAssetHandle`  
> 通用参考：[ResourceSystemDesignGuide.md](../../../../Resources/ResourceSystemDesignGuide.md)（不驱动本项目改方向）

---

## 1. 推荐写法（保存句柄 + 成对卸载）

**原则**：每次 `Load` 成功应对应一次 `Release` 或 `Unload(handle, instance)`。  
C# **没有** C++ 式析构：链式临时句柄出作用域 **不会** 自动 `Release`，AB 会继续占内存。

### 1.1 推荐：字段 + OnDestroy（Prefab / 多资源）

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

### 1.2 同步加载（须保存句柄）

```csharp
IAssetHandle handle = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot");
GameObject go = handle?.Instance;
// 用完后：BundleResLoader.Instance.Unload(handle, go);
```

- `loadPath` 是清单简路径（无扩展名）。

### 1.3 链式写法（仅限短生命周期，须另有 Release 计划）

```csharp
GameObject go = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot")?.Instance;
```

- ⚠️ **不推荐作为默认范式**：未保存 `IAssetHandle` → **无法** `Release` → Resource Ref 泄漏（除非稍后 `UnloadAll()`）。

### 1.4 UniTask 异步加载

```csharp
IAssetHandle handle = await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>("UI/UIRoot");
GameObject go = handle?.Instance;
```

- 须 `await` 到句柄后再取 `Instance`；卸载方式同同步。

### 1.5 UniTask 回调加载

```csharp
BundleResLoader.Instance.LoadUniTaskWithCallback<GameObject>(
    "UI/UIRoot",
    onComplete: handle =>
    {
        GameObject go = handle.Instance;
        // 须在适当时机 handle.Release() 或 Unload(handle, go)
    },
    onFailed: err => Debug.LogError(err)
);
```

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

// OnDestroy: Unload(icon, null); Unload(prefab, go);
```

### 2.2 跨包加载

```csharp
IAssetHandle ui = BundleResLoader.Instance.Load<GameObject>("UI/UIRoot");
GameObject uiGo = ui?.Instance;
```

- 依赖包由清单 `bundles[]` 驱动自动加载。
- 前提：使用包含依赖信息的真实打包清单（非仅 EditorTest 占位产物）。

---

## 3. 卸载资源

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

- ⚠️ 只 `Destroy` **不会** 减少 Resource Ref；实例与 AB 释放仍分离（通用指南痛点 C）。

### 3.4 全量卸载

```csharp
BundleResLoader.Instance.UnloadAll();
```

---

## 4. 使用注意事项

- 每次 `Load` 成功后都应有对应的 `Release/Unload`，避免引用计数悬挂。
- **DestroyInstance / AutoUnload** 延后（见 [主路线.md](./主路线.md) §4）；业务用「保存句柄 + Release」。
- 链式 `Load()?.Instance` 不等于 RAII；局部变量出作用域 **不会** 触发 AB 卸载。
- 当前加载器按主线程调用设计；不要在多线程并发直接调用 `Load/Unload`。
- `LoadUniTaskAsync` 当前是「UniTask 异步入口 + 同步加载内核」，后续再接 inFlight 合并、CDN 与 ref==0 完成丢弃。
