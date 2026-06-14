# ABSystem_Beta 文档索引

> **Docs/**：集中说明、验收标准、跨模块规划。  
> **各子文件夹/**：对应模块的详细设计，与代码同目录维护。

---

## 必读（Docs）

| 文档 | 用途 |
|------|------|
| **[MainRoadmap.md](./MainRoadmap.md)** | **方向 + 排期 + 测试门禁（唯一总纲）** |
| [DesignGoalsAndImplementation.md](./DesignGoalsAndImplementation.md) | 设计基线（禁止区）+ 模块实现进度 |
| [BusinessApiAndCdnPlanning.md](./BusinessApiAndCdnPlanning.md) | CDN / 异步扩展设计细节 |
| [BusinessApiUsageGuide.md](./BusinessApiUsageGuide.md) | 业务侧加载/卸载范式 |
| **本文 § 业务场景总结** | 模块/Prefab/Ref/依赖/路由 **场景速查** |
| [CatalogueReference.md](./CatalogueReference.md) | 清单 `entries` / `bundles` |
| [BundleBuildOptimizationAndTopologyPlan.md](./BundleBuildOptimizationAndTopologyPlan.md) | **依赖拓扑排序 + 构建优化** 实现计划 |
| [BuilderEditorBlueprint.html](./BuilderEditorBlueprint.html) | 打包窗口 **Builder** 页签 UI 原型 |
| [ReportEditorBlueprint.html](./ReportEditorBlueprint.html) | 打包窗口 **Reporter** 页签 UI 原型 |

---

## 按模块（子文件夹）

| 目录 | 文档 | 代码入口 |
|------|------|----------|
| `AbstractAssets/` | [README.md](../AbstractAssets/README.md) | `AbstractResource.cs` |
| `ResLoader/` | [README.md](../ResLoader/README.md)（**含加载侧架构 Mermaid 图**）、[LoaderDesignGuide.md](../ResLoader/LoaderDesignGuide.md) | `Business/` `Bundle/` `Catalogue/` `Router/` |
| `BundleRuleConfig/` | [README.md](../BundleRuleConfig/README.md) | `BuildSetting`、`AssetCatalog` |
| `Editor/` | [README.md](../Editor/README.md) | `BundlePacker`、`BundleBuilder`、`BundleReporter` |
| `AssetPool/` | `PrefabPool.cs`、`PoolSceneRootsUtil.cs` | `CreatPool` / `GetOrCreatPool` / `GetObj` / `ReleaseObj` / `DestroyPool`；按 Active Scene 分池见 [业务API §5.4](./BusinessApiUsageGuide.md) |
| `ABSystemTester/` | [ABSystem_BetaTestCases.md](../../../../BaseLayer/AssetLayer/ABSystemTester/ABSystem_BetaTestCases.md) | `ABLoadSmokeTest.cs` |
| `Assets/Test/AB_Test/` | [集成测试归档.md](../../../../Test/AB_Test/集成测试归档.md)、[测试说明.md](../../../../Test/AB_Test/测试说明.md) | `Myloadtest`、`MyRouterTest`、`LoadApiTestLogCollector` |

---

## 外部参考（不改本项目方向）

| 文档 | 用途 |
|------|------|
| [ResourceSystemDesignGuide.md](../../../Resources/ResourceSystemDesignGuide.md) | Unity 资源系统通用 MUST/SHOULD（对照用） |
| [ApproachComparisonAndLearningGuide.md](../../../../BaseLayer/AssetLayer/ApproachComparisonAndLearningGuide.md) | WindAssetBundle / 学习路线（非 ABSystem_Beta 排期） |

---

## 阅读顺序

1. **[MainRoadmap.md](./MainRoadmap.md)** → 知道做什么、不做什么  
2. **设计目标与实现细节** → 禁止区 + 当前进度  
3. **业务API与CDN规划** + **加载器设计说明** → 加载链路细节  
4. **业务API调用指南** → 业务抄代码  
5. **集成测试归档** → 验收 JSON

---

## 文档维护约定

- 禁止修改 **DesignGoalsAndImplementation.md** 禁止修改区域。  
- **排期与阶段状态** 只维护 **MainRoadmap.md**（及设计目标实现细节区的进度表）。  
- 子模块 `README.md` 不写总排期，只链到主路线。
