# ABSystem_Beta 测试用例

> 针对 `Assets/vFramework/BaseLayer/AssetLayer/ABSystem_Beta` 资源打包与加载系统的测试设计。  
> 被测系统入口：**Unity → vFramework → AssetBundle Packer**  
> 参考文档：[Docs/文档索引.md](../ABSystem_Beta/Docs/文档索引.md)、[业务API与CDN规划.md](../ABSystem_Beta/Docs/业务API与CDN规划.md)

---

## 一、文档说明

### 1.1 测试范围

| 模块 | 被测组件 | 当前实现状态 |
|------|----------|--------------|
| 规则制定器 | `BundleRuleMaker`、`BuildSetting` | 约 75% |
| 打包器 | `BundleBuilder`、`RuleResolver` | 约 65% |
| 清单桥梁 | `CatalogueWriter`、`CatalogueReader`、`AssetCatalog` | `entries` + `bundles[]` 已写入/读取 |
| 抽象资源 | `AbstractResource` | 引用计数 + Release |
| 加载器 | `BundleManager`、`BundleResLoader` | 同步 Load / LoadByPath、依赖预加载 ✅；异步/CDN ❌ |

### 1.2 测试分类

- **打包测试（P-xxx）**：Editor 侧 Build / Clean / Validate / 清单生成
- **加载测试（L-xxx）**：运行时 Load / Release / 引用计数 / 依赖表现

### 1.3 优先级

| 级别 | 含义 |
|------|------|
| P0 | 阻塞主链路，必须通过 |
| P1 | 核心场景与常见边界 |
| P2 | 复杂依赖、大规模、未实现能力的预期行为 |
| P3 | 回归与体验类 |

### 1.4 通用前置条件

1. 测试资源根目录：`Assets/vFramework/BaseLayer/AssetLayer/ABSystemTester/Fixtures/`（需按本文「测试夹具」章节搭建）
2. `BuildSetting` 指向 `DefaultBuildSetting.asset` 或测试专用 SO
3. 每次打包前执行 **清理打包**，避免旧产物干扰
4. 记录 Console 日志，便于对比预期错误信息

---

## 二、测试夹具（Fixtures）设计

建议在 `Assets/vFramework/BaseLayer/AssetLayer/ABSystemTester/Fixtures/` 下准备以下目录结构，供多类用例复用。

```text
Fixtures/
├── Basic/                          # 基础冒烟
│   ├── UI/          (2~3 prefab)
│   └── Audio/       (2~3 clip)
├── Empty/                          # 空文件夹边界
│   └── EmptyFolder/   (无资源)
├── ScriptsOnly/                    # 仅 .cs，应被 RuleResolver 跳过
│   └── Foo.cs
├── Dependencies/                   # 跨包依赖
│   ├── Shared/
│   │   ├── Atlas.spriteatlas / 贴图
│   │   └── CommonMat.mat
│   ├── UI/
│   │   └── Panel.prefab  → 引用 Shared 材质/图集
│   └── Character/
│       └── Hero.prefab   → 引用 Shared + 独立贴图
├── DeepNest/                       # Detailed 规则：多层嵌套
│   ├── A/B/C/D/   (每层 1~2 资源)
├── DuplicateNames/                 # 同名 assetName 不同路径（Detailed/Custom）
│   ├── PackA/icon.png
│   └── PackB/icon.png
├── LargeScale/                     # 大量资源压测（可选生成脚本）
│   ├── Batch_000/ … Batch_099/    (每批 50~100 小资源)
└── CustomMix/                      # Custom 模式混合 buildMode
    ├── FirstPack/ …
    └── RemotePack/ …
```

**依赖链示意（Dependencies 夹具）**：

```text
shared.bundle  ←── ui.bundle (Panel.prefab)
              ←── character.bundle (Hero.prefab)
              ←── (Unity 可能再拆出材质/贴图子依赖)
```

---

## 三、打包测试用例

### 3.1 规则制定器与校验（Validate）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-001 | P0 | 窗口正常打开 | Test → AssetBundle Packer | 窗口显示基本设置、规则区、操作按钮 |
| P-002 | P0 | 目标目录不存在 | `targetDirectory` 设为无效路径，点「开始打包」 | Validate 失败，Console 报错「目标资源目录不存在」 |
| P-003 | P0 | 真机模式空输出路径 | `buildMode=DeviceDebug`，`deviceOutputPath` 清空 | Validate 失败 |
| P-004 | P0 | CDN 模式空输出路径 | `buildMode=CdnHotUpdate`，`cdnOutputPath` 清空 | Validate 失败 |
| P-005 | P1 | Custom 无配置项 | `packingRule=Custom`，`customItems` 为空 | Validate 失败 |
| P-006 | P1 | Custom 含 CDN 项但无 cdn 路径 | 某 customItem.buildMode=CdnHotUpdate，cdnOutputPath 空 | Validate 失败 |
| P-007 | P1 | Save 持久化 | 修改 version/buildNumber，点「保存配置」 | `DefaultBuildSetting.asset` 字段更新，重开窗口仍保留 |
| P-008 | P2 | 平台切换 | 依次选 Windows / Android / iOS 并打包 | 清单 `platform` 字段与选择一致；AB 为目标 BuildTarget |

### 3.2 打包规则：Default（一级子文件夹）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-010 | P0 | 标准 Default 打包 | `targetDirectory=Fixtures/Basic`，规则 Default，DeviceDebug | 每个一级子文件夹生成一个 `{文件夹名}.bundle` |
| P-011 | P1 | 空子文件夹 | `Fixtures/Empty` 含无资源的子文件夹 | 该文件夹 **不** 生成 bundle（`TryAddFolderBuild` 跳过） |
| P-012 | P1 | 仅脚本目录 | `Fixtures/ScriptsOnly` | 不生成 bundle；Console 可能报「没有可打包的内容」 |
| P-013 | P1 | 根目录无一级子文件夹 | target 下直接放资源、无子文件夹 | builds.Count=0，打包失败 |
| P-014 | P2 | 单文件夹大量文件 | Basic/UI 下放 200+ 小贴图 | 仍为一个 `UI.bundle`；清单 entries 数量=资源数 |

### 3.3 打包规则：Detailed（所有嵌套子文件夹）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-020 | P0 | 标准 Detailed | `Fixtures/DeepNest`，Detailed 规则 | 每个子文件夹独立 bundle，命名为 `{相对路径_下划线}.bundle` |
| P-021 | P1 | 深层嵌套 | A/B/C/D 四层 | 生成 `A.bundle`、`A_B.bundle`、`A_B_C.bundle`、`A_B_C_D.bundle`（含根文件夹自身） |
| P-022 | P1 | 同名 assetName | `Fixtures/DuplicateNames` | 不同 bundle 内均可含 `assetName=icon`；entries 中 `assetPath` 唯一 |
| P-023 | P2 | 父子文件夹资源重复引用 | 父文件夹打包含子目录资源（Detailed 每文件夹独立收集） | 同一物理资源出现在多个 bundle 的 assetNames 中（Unity 允许，体积重复—记录实际行为） |

### 3.4 打包规则：Custom（手动配置）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-030 | P0 | 文件夹整包 | 配置一项：路径=Fixtures/Basic/UI，bundleName=ui_pack | 输出 `ui_pack.bundle`，含 UI 下全部非 .cs 资源 |
| P-031 | P0 | 单文件单包 | 配置一项：路径=某单个 .prefab，bundleName=single | 该 bundle 仅含 1 个 asset |
| P-032 | P1 | bundleName 无后缀 | 配置 bundleName=`mybundle` | 自动补全为 `mybundle.bundle` |
| P-033 | P1 | 无效 assetPath | 路径不存在或空 | 该项被跳过，不加入 builds |
| P-034 | P1 | 混合 buildMode | 项 A=DeviceDebug，项 B=CdnHotUpdate | A 输出到 deviceOutputPath，B 输出到 cdnOutputPath；各写一份清单（**注意：后写覆盖前写，需验证实际行为**） |
| P-035 | P2 | 重复 bundleName | 两项配置相同 bundleName | 记录 Unity BuildPipeline 行为（覆盖/报错/合并） |

### 3.5 打包模式（BuildMode）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-040 | P0 | 编辑器测试 | buildMode=EditorTest | **不** 调用 BuildPipeline；输出目录 **无** 新 `.bundle` |
| P-041 | P0 | 编辑器测试仍写清单 | EditorTest + Basic 夹具 | `AssetCatalog.json` 仍更新；entries 与规则一致 |
| P-042 | P0 | 真机模式/首包 | buildMode=DeviceDebug | `deviceOutputPath` 下生成真实 `.bundle` + Unity `.manifest` |
| P-043 | P0 | CDN 联网 | buildMode=CdnHotUpdate | `cdnOutputPath`（默认 `Bundles/CDN`）下生成 AB |
| P-044 | P1 | EditorTest 的 bundleRoot | 对比清单中 `bundleRoot` 字段 | 当前实现仍指向 deviceOutputPath（占位行为，记录即可） |
| P-045 | P2 | 连续切换模式打包 | 同一夹具先 EditorTest 再 DeviceDebug | 第二次出现真实 bundle；清单 buildMode 字段更新 |

### 3.6 清单（Catalogue）生成

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-050 | P0 | 双份 JSON 输出 | DeviceDebug 打包后 | 工程内 `BundleRuleConfig/Catalogue/AssetCatalog.json` 与 `{bundleRoot}/Catalogue/AssetCatalog.json` 内容一致 |
| P-051 | P0 | entries 字段完整 | 抽查 10 条 entry | 含 `assetPath`、`bundleName`、`assetName`；assetName=文件名无扩展名 |
| P-052 | P0 | 元数据一致 | 对比 BuildSetting 窗口 | `version`、`buildNumber`、`platform`、`packingRule`、`buildMode` 与配置一致 |
| P-053 | P1 | JSON 可解析 | 用外部工具或 JsonUtility 反序列化 | 无格式错误；能还原为 `AssetCatalog` |
| P-054 | P1 | bundleRoot 绝对路径 | 查看 JSON 中 bundleRoot | 为本次输出目录的绝对路径 |
| P-055 | P2 | bundles 依赖表（未实现） | DeviceDebug 打包 Dependencies 夹具 | **当前** JSON 无 `bundles` 字段；实施后：与 `ui.bundle.manifest` 的 Dependencies 一致 |
| P-056 | P3 | 清单体积 | LargeScale 10000 entries | JSON 体积与加载耗时记录基线；二进制化后对比（未来） |

### 3.7 清理（Clean）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-060 | P0 | 标准清理 | 先 DeviceDebug 打包，再「清理打包」 | deviceOutputPath、cdnOutputPath 下 `.bundle`/`.manifest` 删除 |
| P-061 | P0 | Catalogue 清理 | Clean 后 | 工程内 `AssetCatalog.json` 删除 |
| P-062 | P1 | 运行时 Catalogue 目录 | Clean 后 | `{bundleRoot}/Catalogue/` 删除 |
| P-063 | P1 | 无 orphan .meta | Clean 后刷新 Project | Console 无 meta 丢失警告 |
| P-064 | P2 | 空目录输出路径 | 从未打包直接 Clean | 不报错，正常完成 |
| P-065 | P2 | 输出路径含非 AB 文件 | StreamingAssets 下手动放 `readme.txt` | Clean **仅** 删 `.bundle`/`.manifest`/Catalogue，保留其它文件 |

### 3.8 复杂依赖与 Unity 打包行为

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-070 | P1 | UI 引用 Shared 材质 | Dependencies 夹具，Default 规则（Shared/UI/Character 各一包） | `ui.bundle.manifest` 的 Dependencies 含 `shared.bundle`（或 Unity 拆分的依赖包名） |
| P-071 | P1 | 循环引用资源 | A.mat 引用 B.prefab，B 又引用 A（刻意构造） | 打包成功或 Unity 报错—记录实际行为 |
| P-072 | P1 | 同资源多包引用 | Detailed 下父子文件夹均含同一贴图引用 | 观察 Unity 是否 duplicate 进多包或自动抽公共包 |
| P-073 | P2 | Shader 变体 | 材质使用不同 Shader | 打包成功；manifest 依赖含 shader 相关 bundle（若有） |
| P-074 | P2 | SpriteAtlas | Shared 使用 SpriteAtlas，UI Prefab 引用 | atlas 与 UI 包依赖关系正确写入 manifest |
| P-075 | P2 | 场景 AssetBundle | Custom 打包 `.unity` 场景 | 场景可打进 bundle；entries 含场景路径 |

### 3.9 大规模与性能（打包侧）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-080 | P2 | 100 个 bundle | LargeScale：100 个一级文件夹各 10 资源，Default | 打包完成；bundle 数=100；清单 entries≈1000 |
| P-081 | P2 | 单 bundle 5000 资源 | 一个文件夹内 5000 小纹理 | 打包不崩溃；记录耗时与 bundle 体积 |
| P-082 | P2 | Detailed 爆炸 | 深度 5、每层 3 子文件夹 | bundle 数量=所有文件夹数；评估是否超出业务预期 |
| P-083 | P3 | 全量重复打包 | 相同配置连续 Build 3 次 | 产物可被覆盖；CRC/hash 稳定（资源未改时） |
| P-084 | P3 | 增量打包（未实现） | 改 1 个资源后再次 Build | **当前** 为全量；记录耗时，待增量功能上线后补充对比 |

### 3.10 边界与异常

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| P-090 | P1 | 中文/空格路径 | 文件夹名 `UI 界面` 或含中文资源名 | 打包成功；清单 assetPath 正确 |
| P-091 | P1 | 特殊字符 bundleName | Custom：`ui-pack_v2.bundle` | 正常输出 |
| P-092 | P1 | 超长路径 | 嵌套路径接近 Windows MAX_PATH | 记录成功/失败边界 |
| P-093 | P2 | 打包中改资源 | Build 过程中修改某 prefab | 记录 Unity 是否一致或需重新 Build |
| P-094 | P2 | 输出目录只读 | deviceOutputPath 设为无写权限目录 | 明确失败提示 |
| P-095 | P2 | BuildSetting=null | 代码层调用 `BundleBuilder.Build(null)` | 报错「BuildSetting 为空」 |

---

## 四、加载测试用例

> 加载侧建议用 `ABSystemTester` 下 MonoBehaviour 测试脚本或 PlayMode Test 执行。  
> 初始化：`BundleResLoader.Init(bundleRootPath)`，`bundleRootPath` 与清单 `bundleRoot` 或 StreamingAssets 一致。

### 4.1 基础加载与缓存

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| L-001 | P0 | 同步加载 Prefab | `Load<GameObject>("UI.bundle", "Panel")` | 返回非 null `AbstractResource`；`GetAsset<GameObject>()` 有效 |
| L-002 | P0 | Instantiate | 对上例调用 `Instantiate()` | 场景中出现实例；与直接拖 Prefab 表现一致 |
| L-003 | P0 | 重复 Load 同一资源 | 连续两次 Load 相同 bundleName+assetName | 第二次走缓存；Resource 层 Ref 增加 |
| L-004 | P1 | Load 不存在 bundle | 错误 bundleName | Console 报错「Bundle load failed」；返回 null |
| L-005 | P1 | Load 不存在 assetName | 正确 bundle，错误 assetName | Console 报错「Asset load failed」；ReleaseBundle 回滚 |
| L-006 | P1 | 错误泛型类型 | `Load<Texture2D>` 但实际为 AudioClip | `GetAsset<Texture2D>()` 为 null |
| L-007 | P2 | EditorTest 模式产物 | 仅 EditorTest 清单、无真实 bundle | Load 失败；验证错误信息清晰 |

### 4.2 引用计数与卸载

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| L-010 | P0 | 单次 Release | Load 一次后 `Release()` 一次 | Resource Ref=0，从 resourceDic 移除；对应 bundle Ref=0 并 Unload |
| L-011 | P0 | 多次引用 Release | Load 两次（或 AddReference 两次） | 需 Release 两次才真正 Unload bundle |
| L-012 | P1 | 重复 Release | Ref 已为 0 再 Release | 不崩溃；ReduceReference 保护（Ref 不低于 0） |
| L-013 | P1 | UnloadAll | 加载多个资源后 `loader.UnloadAll()` | 全部 AbstractResource 卸载；`BundleManager` 清空 |
| L-014 | P2 | Instantiate 与 Release 独立 | Instantiate 后仅 Release 抽象资源 | 已实例化 GameObject 仍存在（设计约定：Destroy 与 Release 无关） |
| L-015 | P2 | 先 Destroy 实例再 Release | 正常业务顺序 | 无泄漏报错 |

### 4.3 跨包依赖（关键）

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| L-020 | P0 | 仅 Load 主包 UI | Dependencies 夹具，只 Acquire `UI.bundle`，Load Panel | **当前实现**：可能 pink/material 丢失；**目标实现**：自动加载 shared 依赖 |
| L-021 | P1 | 手动先 Load 依赖包 | 先 Load Shared 内资源，再 Load UI Panel | Panel 显示正常 |
| L-022 | P1 | 卸载顺序：先卸依赖 | UI Panel 仍持有，Release Shared | 记录材质是否丢失（依赖包被 Unload(true) 的影响） |
| L-023 | P2 | 卸载顺序：先卸主包 | Release UI 后再 Release Shared | 无崩溃；无 dangling 引用 |
| L-024 | P2 | bundles 表驱动（未来） | 实现 `AcquireBundleWithDependencies` 后重测 L-020 | 仅 Load UI 即显示正常 |
| L-025 | P2 | 依赖链 A→B→C | 三层 bundle 依赖 | 递归加载顺序正确；Ref 计数各包独立正确 |

### 4.4 路径与根目录

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| L-030 | P0 | 默认 StreamingAssets | Init 传 null 或 StreamingAssets 路径 | LoadFromFile 路径正确 |
| L-031 | P1 | 自定义 bundleRoot | CDN 输出目录绝对路径 Init | 从 `Bundles/CDN` 加载成功 |
| L-032 | P1 | bundleName 大小写 | 清单与磁盘文件名大小写不一致 | 记录 Windows/Android/iOS 差异 |
| L-033 | P2 | 清单驱动 LoadByPath（未来） | `LoadByPath("Assets/.../Panel.prefab")` | 查 entries 后等价于 L-001 |

### 4.5 大规模加载

| ID | 优先级 | 场景 | 操作步骤 | 预期结果 |
|----|--------|------|----------|----------|
| L-040 | P2 | 顺序加载 1000 资源 | LargeScale 清单 | 无 OOM；记录总耗时 |
| L-041 | P2 | 加载后全部 Release | 对上例逐个 Release | 内存回落至合理水平；无 bundle 残留 |
| L-042 | P2 | 100 个不同 bundle 各 Load 1 资源 | | loadedBundles.Count=100；内存监控 |
| L-043 | P3 | 同一 bundle 1000 次 Load 同一 asset | | 仅 1 个 AbstractResource；Ref=1000；Release 1000 次后卸载 |
| L-044 | P3 | 并发 Load（若后续有 Async） | 多协程同时 Load | 无竞态重复创建；当前同步 API 可测多线程不适用 |

### 4.6 异步与 API 占位（当前未实现）

| ID | 优先级 | 场景 | 预期（实现后） |
|----|--------|------|----------------|
| L-050 | P3 | LoadAsync | 回调/await 返回与同步一致 |
| L-051 | P3 | LoadWithCallback | 默认异步，完成回调 |
| L-052 | P3 | PreLoad 模块 | 按模块批量预热 bundle |

---

## 五、端到端场景（E2E）

| ID | 优先级 | 场景 | 步骤摘要 | 预期 |
|----|--------|------|----------|------|
| E-001 | P0 | 开发迭代闭环 | EditorTest 改规则 → 看清单 → DeviceDebug 真打 → Play 加载 | 清单与 AB 一致，Play 可加载 |
| E-002 | P1 | 首包 + 热更分包 | Custom：核心=DeviceDebug，扩展=CdnHotUpdate | 两路径均有包；Play 时 Init 需支持多 root 或拷贝策略（**待产品设计**） |
| E-003 | P1 | Clean 后重建 | Clean → 全量 Build → Load | 与首次一致 |
| E-004 | P2 | 版本升级 | buildNumber+1 重打包，模拟热更 | 新清单 version/buildNumber 更新；旧缓存 AB 行为（未来热更模块） |

---

## 六、测试执行记录模板

```text
用例 ID：
执行日期：
Unity 版本：
执行人：
BuildSetting 快照：（platform / buildMode / packingRule / version）
结果：Pass / Fail / Blocked / N/A
实际现象：
Console 摘要：
关联 Bug：
```

---

## 七、已知限制与测试策略

| 限制 | 对测试的影响 | 建议 |
|------|--------------|------|
| `bundles[]` 未写入 JSON | L-020 预期失败或表现异常 | 标记 Blocked；依赖实现后改为 P0 |
| EditorTest 不产出 AB | 加载测试需 DeviceDebug/CDN 产物 | 加载用例统一先跑真机模式打包 |
| Custom 混合模式多次 Write 清单 | P-034 可能只保留最后一次 | 单独记录实际覆盖行为，必要时提缺陷 |
| 无增量打包 | P-084 仅记录全量耗时 | 功能上线后补充对比用例 |
| 无 CatalogueReader / LoadByPath | L-033 Blocked | 路由器实现后启用 |
| `CollectAssetPaths` 跳过 .cs | 脚本不能单独进包 | 若业务需要 ScriptableObject 配置，用 .asset 测 |

---

## 八、推荐执行顺序（冒烟 → 全量）

1. **冒烟（约 30 分钟）**：P-001、P-010、P-040~P-043、P-050~P-052、P-060、L-001~L-003、L-010  
2. **规则覆盖（约 1 小时）**：P-020~P-022、P-030~P-032、P-070~P-071  
3. **加载与依赖（约 1 小时）**：L-020~L-023、L-011~L-013  
4. **边界（约 30 分钟）**：P-011~P-013、P-090~P-092、L-004~L-006  
5. **压测（可选）**：P-080~P-082、L-040~L-042  

---

## 九、相关路径速查

| 项 | 路径 |
|----|------|
| 被测系统 | `Assets/vFramework/BaseLayer/AssetLayer/ABSystem_Beta/` |
| 测试文档与夹具 | `Assets/vFramework/BaseLayer/AssetLayer/ABSystemTester/` |
| 默认配置 | `Assets/vFramework/BaseLayer/AssetLayer/ABSystem_Beta/BundleRuleConfig/Setting/DefaultBuildSetting.asset` |
| 清单（工程内） | `Assets/vFramework/BaseLayer/AssetLayer/ABSystem_Beta/BundleRuleConfig/Catalogue/AssetCatalog.json` |
| 首包输出（默认） | `Assets/StreamingAssets/` |
| CDN 输出（默认） | `Bundles/CDN/` |
| 菜单入口 | **vFramework → AssetBundle Packer** |

---

*文档版本：1.0 | 对 ABSystem_Beta 打包侧 MVP + 加载雏形对齐*
