---
name: vframework-documentation
description: >-
  Create or update vFramework / ABSystem_Beta documentation. Use when adding
  markdown docs, roadmaps, TODO lists, planning docs, or before creating any new
  file under BaseAssetSys/Docs or module READMEs.
---

# vFramework 文档编写

## 何时使用

- 用户或任务要求写文档、路线图、TODO、设计说明。
- 即将在 `Assets/vFramework/**` 下新建 `.md` 文件。
- 合并/整理排期、TODO、多份 Plan 文档。

## 第一步：查重（禁止跳过）

1. 打开 `Assets/vFramework/BaseFramework/BaseAssetSys/Docs/DocumentIndex.md`。
2. 阅读 § **文档分类与合并规则** 与 § **已有专题文档**。
3. 用关键词在 `Docs/` 搜索（如「CDN」「对象池」「排期」「TODO」）。

**若已有同类文档 → 扩写该文档，不新建平行文件。**

## 第二步：选对容器

| 内容 | 目标文件 |
|------|----------|
| 排期 P0–P3、延后、完成状态 | `Docs/MainRoadmap.md` §4 |
| 源码 `TODO` 登记 | `Docs/MainRoadmap.md` §8 |
| 加载侧优化设计（AssetReference、池） | `Docs/LoaderOptimizationPlan.md`（无状态表） |
| CDN / 异步设计 | `Docs/BusinessApiAndCdnPlanning.md` |
| Bundle 拓扑/构建优化设计 | `Docs/BundleBuildOptimizationAndTopologyPlan.md` |
| 业务 API 范例 | `Docs/BusinessApiUsageGuide.md` |
| 单机 / 资源热更接入 | `Assets/vFramework/Docs/Guides/StandaloneAndResourceHotfixGuide.md` |
| 框架总览 | `Assets/vFramework/Docs/Overview/`、`Docs/README.md` |
| Ref 逐步追踪 | `Docs/RefCountAppendix.md` |
| 模块说明 | 对应目录 `README.md` |

## 第三步：禁止事项

- 不要新建 `TODO.md`、`NextSteps.md`、第二份 `MainRoadmap`。
- 不要在 `*Plan.md` 里维护带 ✅/❌ 的「实施排期」表（链到 MainRoadmap）。
- 不要修改 `DesignGoalsAndImplementation.md` **禁止修改区域**。
- 不要在子模块 README 写总排期段落（只链 MainRoadmap）。

## 第四步：联动更新

完成文档后检查：

- [ ] `DocumentIndex.md` 必读表（若新常驻文档）
- [ ] `MainRoadmap.md` §7 文档地图（若新常驻文档）
- [ ] `MainRoadmap.md` §8（若新增代码 TODO）
- [ ] 相关 Plan 文首「排期见 MainRoadmap §x」

## 快速路径

- 唯一总纲：`Docs/MainRoadmap.md`
- 索引与门禁：`Docs/DocumentIndex.md`
- Cursor 规则：`.cursor/rules/documentation-governance.mdc`
