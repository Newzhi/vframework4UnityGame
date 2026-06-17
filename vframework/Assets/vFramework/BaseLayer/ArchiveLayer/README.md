# ArchiveLayer

> 路径：`BaseLayer/ArchiveLayer/`  
> 命名空间：`BaseLayer.Archive`

通用存档层：槽位 CRUD + 手动/自动存档。payload 为 opaque `byte[]`，序列化由热更 `ISaveDataCollector` / `ISaveDataApplier` 负责。

## Bootstrap

```csharp
modules.AddModule(new ArchiveModule(
    collector: new MySaveCollector(),
    applier: new MySaveApplier(),
    manualSlotCount: 3,
    autoSaveIntervalSeconds: 120f));
```

需已注册 `GameTimeModule` 时，元数据会自动写入 `PlayTime` / `ChapterId`。

## CRUD

```csharp
_archive = services.Get<IArchiveService>();

_archive.ListSlots();
_archive.SaveManual(0, "存档1");
_archive.SaveAuto();
_archive.Load(ArchiveSlotId.Manual(0));
_archive.Delete(ArchiveSlotId.Auto);
_archive.DeleteAll();
```

## 存储布局

```text
{persistentDataPath}/Archives/
├── index.json       // 槽位元数据列表
├── manual_0.bin     // payload
├── manual_1.bin
└── auto_0.bin
```

## 热更层需实现

| 接口 | 职责 |
|------|------|
| `ISaveDataCollector` | `Collect()` → `byte[]` |
| `ISaveDataApplier` | `Apply(byte[])` 读档 |
| `IAutoSavePolicy` | 可选：屏蔽加载屏/剧情时的自动存 |

## 目录

```text
ArchiveLayer/
├── Interfaces/
└── Impt/
```
