# DebugLogger 使用指南

> 代码入口：`BaseLogSys/DebugLogger.cs`、`DebugLoggerPaths.cs`  
> 与 `AssetRefTraceLogger`（资源引用 Trace / JSONL）互补：本类为**通用业务调试日志**，用法对齐 `Debug.Log`。

---

## 1. 做什么

| 能力 | 说明 |
|------|------|
| Console 输出 | 默认与 `Debug.Log` / `LogWarning` / `LogError` 行为一致 |
| 真机落盘 | 非 Editor 默认写入 `Logs` 目录下的 `.log` 文件 |
| 平台路径 | 自动按当前运行平台解析可写目录（见 §3） |
| 自定义目录 | 启动前可指定绝对路径，覆盖自动解析 |

测试模块（`LoadApiTestLogCollector`、`ComprehensiveTestLogExporter`）各自维护结构化导出；新业务调试建议优先用 `DebugLogger`，专项 Trace 仍用 `AssetRefTraceLogger`。

---

## 2. 快速开始

```csharp
void Awake()
{
    // 可选：真机测试前指定目录与会话名
    DebugLogger.Configure(
        logDirectory: null,           // null = 自动解析（推荐）
        tag: "Boot",                    // 文件名 game_Boot_yyyyMMdd_HHmmss.log
        enableFileOutput: true,         // Editor 下也可手动开启写文件
        enableConsoleMirror: true);

    DebugLogger.Log("GameBootstrap start");
    DebugLogger.LogWarning("CDN not ready, use local cache", tag: "CDN");
    DebugLogger.LogError("Init failed: catalogue missing");
    DebugLogger.LogFormat("Boot", "playerId={0} scene={1}", playerId, sceneName);
    DebugLogger.LogException(ex, tag: "Boot");

    // 真机拉日志前可打路径
    DebugLogger.Flush();
    DebugLogger.Log("Log dir: " + DebugLogger.GetLocationHint());
}
```

---

## 3. 日志目录策略

解析顺序（与 AB_Test / 综合测试一致）：

1. **自定义目录** — `Configure(logDirectory: ...)` 或 `SetLogDirectory(...)`
2. **Bundle 根目录 / Logs** — `StreamingAssets/{平台}/Logs`（可写时优先，便于与 AB 同目录调试）
3. **persistentDataPath / vFramework / Logs** — 真机常用回退路径
4. **Editor 工程 Assets/Logs** — 仅 Editor 最后回退

### 各平台典型位置

| 平台 | 典型路径 |
|------|----------|
| Android | `/storage/emulated/0/Android/data/{包名}/files/vFramework/Logs/` |
| iOS | App Container → `Library/Application Support/.../vFramework/Logs/` |
| Windows 独立包 | `{exe同目录或StreamingAssets平台目录}/Logs` 或 `%USERPROFILE%/AppData/...` |
| Unity Editor | `Assets/StreamingAssets/{平台}/Logs` 或 `Assets/Logs` |

拉取示例：

```bash
# Android
adb pull "/storage/emulated/0/Android/data/com.your.game/files/vFramework/Logs" ./device_logs
```

运行时可通过 API 查询：

```csharp
DebugLogger.GetLogDirectory();           // 当前解析到的目录
DebugLogger.GetPersistentLogDirectory(); // persistent 回退目录
DebugLogger.GetLocationHint();           // 含 adb 提示的说明字符串
DebugLogger.ActiveLogFilePath;           // 当前会话 .log 绝对路径
```

---

## 4. API 一览

### 4.1 配置

| API | 说明 |
|-----|------|
| `Enabled` | 总开关；Release 非 Development 构建默认 `false` |
| `MirrorToUnityConsole` | 是否同步 Unity Console，默认 `true` |
| `WriteToFile` | 是否写磁盘；Editor 默认 `false`，真机默认 `true` |
| `Configure(...)` | 启动前一次性配置目录 / tag / 写文件 / Console |
| `SetLogDirectory(path)` | 仅改目录并重开会话文件 |
| `Flush()` | 打一条当前 log 文件路径到 Console |

### 4.2 输出（对齐 Debug）

| API | 对应 Unity API |
|-----|----------------|
| `Log(message)` | `Debug.Log` |
| `Log(message, context)` | `Debug.Log` + Object 上下文 |
| `Log(message, tag)` | 带模块标签的 Log |
| `LogWarning(...)` | `Debug.LogWarning` |
| `LogError(...)` | `Debug.LogError` |
| `LogFormat(format, args)` | 格式化 Info |
| `LogFormat(tag, format, args)` | 带 tag 的格式化 Info |
| `LogException(ex, tag?)` | 异常堆栈 |

### 4.3 日志行格式

```
[2026-06-19 14:30:22.123][INFO][Boot] GameBootstrap start
[2026-06-19 14:30:23.456][WARNING][CDN] CDN not ready
[2026-06-19 14:30:24.789][ERROR] Init failed
```

文件首段为会话头（Unity 版本、平台、包名、路径等），之后每行一条记录。

---

## 5. 默认开关（编译期）

| 环境 | `Enabled` | `WriteToFile` |
|------|-----------|---------------|
| Editor / Development Build | `true` | Editor: `false`；真机: `true` |
| Release | `false` | `false` |

Release 包若需真机日志，在入口显式开启：

```csharp
#if !UNITY_EDITOR
DebugLogger.Enabled = true;
DebugLogger.WriteToFile = true;
#endif
```

---

## 6. 与 AssetRefTraceLogger 的分工

| 类 | 用途 | 输出 |
|----|------|------|
| `DebugLogger` | 通用业务调试、启动流程、异常 | 人类可读 `.log` |
| `AssetRefTraceLogger` | Resource / Bundle / Pool 引用计数 Trace | Console + 真机 `.jsonl` |

二者目录均落在 `Logs` 下，文件名前缀不同（`game_*` vs `ref_trace_*`），互不覆盖。

---

## 7. 迁移建议（自测模块）

现有测试代码可逐步替换 `Debug.Log` 为 `DebugLogger.Log`，保留各自 JSON / 结构化 Collector；会话结束时额外调用：

```csharp
DebugLogger.Log("Test session exported: " + collectorPath, tag: "Test");
DebugLogger.Flush();
```

无需修改 `LoadApiTestLogCollector` 的 JSON 格式；`DebugLogger` 负责**运行时连续文本日志**，Collector 负责**用例结果归档**。
