# ABundle 运行时测试

基于 **ABundleLoader** 的全场景加载与内存泄漏检测。与 `ResTest/` 下的旧 AB 学习演示并存，本目录专用于新框架验证。

## 前置条件

1. 在 Unity 菜单 **vFramework → AssetKit → ABundleBuilder** 中配置规则并 **打包**（Windows 平台）。
2. 确认输出目录存在 Catalog，例如：
   `Assets/StreamingAssets/AssetBundles/Windows/AssetCatalog.json`

## 快速开始

1. 新建空场景，创建 GameObject，挂载 **`ABundleLoadTestRunner`**。
2. 进入 Play 模式，点击屏幕左上角 **「开始全场景测试」**。
3. 测试结束后查看 log 目录（默认 **`Assets/Test/ABundleTest/Logs/`**）。

也可在 Inspector 中右键组件 → **Run All Tests**。

## 脚本说明

| 脚本 | 作用 |
|------|------|
| `ABundleMemorySnapshot` | 内存与包引用快照数据结构 |
| `ABundleMemorySampler` | 采集 Profiler 内存 + Loader 包 RefCount |
| `ABundleMemoryLogger` | 写入分步快照与 `*_summary.txt` 汇总 |
| `ABundleLoadTestRunner` | 一键执行五阶段测试 |

## 测试阶段

1. **初始化** — 加载规则与 Catalog
2. **全 Location 正确性** — 遍历 Catalog 逐条 LoadHandle / Release
3. **重复加载** — 同一 location 加载 3 次，验证 RefCount 与释放
4. **压力循环** — 默认 10 轮「Load 全部 → Release 全部 → GC」
5. **泄漏判定** — 对比基线与末轮内存；超出阈值（默认 512KB）标记 `LEAK_SUSPECT`

## Log 格式

- `{sessionId}_{tag}_{序号}.txt` — 各阶段内存快照
- `{sessionId}_summary.txt` — 汇总报告（含 PASS / LEAK_SUSPECT）

可在 Inspector 中修改 `logRoot` 指向其他目录（支持 `Assets/...` 或绝对路径）。

## 相关工具

- **ABundleAnalyzer** — Editor 下查看 Manifest 依赖与 Location
- **ABundleDemoRunner** — 单资源 Load/Unload 演示（支持 ScopeLoader）

## 演进

后续计划见 [`ABundleLayer演进指导.md`](../../vFramework/BaseLayer/AssetLayer/ABundleLayer/Docs/ABundleLayer演进指导.md)。
