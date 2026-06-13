# AB 打包产物与字段说明

> 本文只讲一件事：**Unity 打完 AssetBundle 之后，磁盘上会多出哪些文件？每个字段是什么意思？依赖记在哪里？**  
> 例子全部用虚构场景（角色、UI、贴图），不绑定任何具体项目。

---

## 一、先建立直觉：打完包像「快递分拣」

想象你把游戏资源分成几个快递箱：

| 箱子（bundle） | 里面装什么 |
|----------------|------------|
| `characters/hero` | 英雄模型、动画 |
| `textures/icons` | 各种小图标贴图 |
| `ui/login` | 登录界面 Prefab |

打包完成后，仓库（输出目录）里会出现：

1. **真正的箱子** → 无扩展名的二进制文件，或 `xxx.bundle`
2. **每个箱子的说明书** → `xxx.manifest`（文本，给人看）
3. **总仓库目录** → 与输出文件夹同名的主 Manifest（记录「哪个箱子依赖哪个箱子」）

运行时加载资源，本质就是：**先找到对的箱子 → 打开箱子 → 取出里面的某件物品**。

---

## 二、打完包后，目录一般长什么样

假设输出目录叫 `AssetBundles`（名字可以自定义，常见写法）：

```text
AssetBundles/                         ← 输出根目录
├── AssetBundles                      ← 主 Manifest 包（二进制，无扩展名）
├── AssetBundles.manifest             ← 主 Manifest 的文本版
│
├── characters/
│   ├── hero                          ← 对应标签 characters/hero
│   └── hero.manifest
│
├── textures/
│   ├── icons
│   └── icons.manifest
│
├── ui/
│   ├── login
│   └── login.manifest
│
└── （可选）Catalogue/
    └── AssetCatalog.json             ← 部分项目会额外生成自定义清单
```

说明：

- **每个资源包**通常有一对：`包文件` + `包名.manifest`
- **主 Manifest**的名字 = **输出文件夹的名字**（上例中都是 `AssetBundles`）
- 包文件可能没有 `.bundle` 后缀，取决于你怎么配置；逻辑上都叫「AB 包」

---

## 三、三种「名字」最容易混，先分清

加载资源时，你会同时遇到三种不同的名字：

| 类型 | 举例 | 谁在用 | 通俗理解 |
|------|------|--------|----------|
| **工程路径** | `Assets/Prefabs/UI/LoginPanel.prefab` | 仅编辑器、打包配置 | 资源在工程里的「家庭住址」 |
| **包名 / 磁盘路径** | `ui/login` | `LoadFromFile` | 快递箱的「编号」 |
| **包内资源名** | `LoginPanel` | `LoadAsset` | 箱子里的「物品标签」，多为文件名（无扩展名） |

**记忆口诀：**

- 找箱子 → 用**包名**
- 拿东西 → 用**包内资源名**
- 改资源、查规则 → 用**工程路径**

---

## 四、`.bundle`（或同名无扩展名文件）是什么

### 它是什么

把 Unity 资源（Prefab、贴图、材质、场景等）序列化后的**二进制容器**。

### 它不是什么

- 不是文本，不能直接打开看内容
- 不是「一个资源一个文件」——一个包里可以装很多资源
- 里面**没有**方便人类阅读的目录；要知道装了什么，得看 `.manifest` 或自定义清单

### 运行时怎么用

```text
LoadFromFile(".../ui/login")     → 把整个「登录界面箱子」搬进内存
bundle.LoadAsset("LoginPanel")   → 从箱子里取出 LoginPanel 这件货
```

若是 Prefab，通常还要 `Instantiate` 才能出现在场景里。

---

## 五、单个包的 `.manifest`：每个字段干什么

每个 AB 包旁边都会有一个同名的 `.manifest` 文本文件。  
下面是一份**虚构的** `login.manifest` 片段，并逐段解释。

```yaml
ManifestFileVersion: 0
CRC: 1234567890
Hashes:
  AssetFileHash:
    serializedVersion: 2
    Hash: a1b2c3d4e5f6...
  TypeTreeHash:
    serializedVersion: 2
    Hash: 9f8e7d6c5b4a...
  IncrementalBuildHash:
    serializedVersion: 2
    Hash: a1b2c3d4e5f6...
HashAppended: 0
ClassTypes:
- Class: 28
  Script: {instanceID: 0}
- Class: 114
  Script: {fileID: 11500000, guid: xxxxx, type: 3}
Assets:
- Assets/Prefabs/UI/LoginPanel.prefab
- Assets/Prefabs/UI/LoginButton.prefab
Dependencies:
- .../textures/icons
```

### 字段说明表

| 字段 | 作用 | 通俗说法 |
|------|------|----------|
| `ManifestFileVersion` | Manifest 格式版本 | 说明书版本号，一般不用管 |
| `CRC` | 整个 AB 包的循环冗余校验值 | 快递封条上的校验码，加载时可用来验货 |
| `Hashes.AssetFileHash` | 包内资源数据的哈希 | 判断「箱子里东西」有没有变 |
| `Hashes.TypeTreeHash` | 类型树哈希 | 判断 Unity 序列化结构有没有变 |
| `Hashes.IncrementalBuildHash` | 增量构建哈希 | 打包工具用来决定「要不要重打这个包」 |
| `HashAppended` | 包名是否追加了 Hash 后缀 | `0` = 没追加；`1` = 文件名可能带一长串 Hash |
| `ClassTypes` | 本包序列化用到的 Unity 类型列表 | 箱子里可能有哪些「物种」（贴图、Prefab、脚本组件等） |
| `Assets` | **打进这个包的所有工程资源路径** | 装箱清单：这个箱子里具体有哪些货 |
| `Dependencies` | **本包依赖的其他 AB 包路径** | 拆箱前还得先把哪些「前置箱子」搬过来 |

### `Assets` 和 `Dependencies` 最重要

- **`Assets`**：回答「这个包里有什么」
- **`Dependencies`**：回答「加载这个包之前，还得先加载谁」

举例：`LoginPanel.prefab` 用到了 `icons` 包里的贴图。  
打包时如果贴图在 `textures/icons`，登录界面在 `ui/login`，则：

- `ui/login.manifest` 的 `Dependencies` 里会出现 `textures/icons`
- `textures/icons.manifest` 的 `Dependencies` 通常是 `[]`（空列表）

---

## 六、主 Manifest：全仓库的「依赖总表」

除了每个包自己的小说明书，Unity 还会生成一份**总目录**，名字与输出文件夹相同。

### 两种形态

| 形态 | 文件 | 用途 |
|------|------|------|
| 文本版 | `AssetBundles.manifest` | 开发时肉眼查看、做 CI 检查 |
| 二进制版 | `AssetBundles`（无扩展名） | 运行时加载，得到 `AssetBundleManifest` 对象 |

### 文本版长什么样（虚构示例）

```yaml
ManifestFileVersion: 0
CRC: 9876543210
AssetBundleManifest:
  AssetBundleInfos:
    Info_0:
      Name: characters/hero
      Dependencies: {}
    Info_1:
      Name: textures/icons
      Dependencies: {}
    Info_2:
      Name: ui/login
      Dependencies:
        Dependency_0: textures/icons
```

### 字段说明

| 字段 | 作用 |
|------|------|
| `AssetBundleManifest` | 整棵「包关系树」的根 |
| `AssetBundleInfos` | 所有包的条目集合 |
| `Info_N.Name` | 某一个 AB 包的名称 |
| `Info_N.Dependencies` | 该包的**直接依赖**；`{}` 表示没有依赖 |
| `Dependency_0`, `Dependency_1`… | 依赖包名列表（有序） |

### 运行时怎么用（逻辑，非绑定代码）

```text
1. 加载主 Manifest 包 AssetBundles
2. 取出里面的 AssetBundleManifest 对象
3. 对目标包 ui/login 调用 GetAllDependencies
4. 先按顺序加载 textures/icons 等依赖包
5. 最后加载 ui/login
6. 再 LoadAsset("LoginPanel")
```

### 常用 API（Unity 自带）

| API | 得到什么 |
|-----|----------|
| `GetAllAssetBundles()` | 所有包名 |
| `GetDirectDependencies(包名)` | 只查「直接依赖」 |
| `GetAllDependencies(包名)` | 查「全部依赖」（含间接依赖） |

**单包、无跨包引用时**，可以跳过主 Manifest，直接 `LoadFromFile` 目标包。  
一旦存在跨包引用（UI 引用公共贴图、角色引用公共 Shader 等），**必须先处理依赖**。

---

## 七、依赖是怎么产生的（不是手写的）

很多人以为要在配置文件里手写「A 依赖 B」。  
实际上，**Unity 打包时根据资源引用关系自动算出来**。

### 简单故事

1. 你只给 `LoginPanel.prefab` 打了标签，放进 `ui/login` 包
2. 这个 Prefab 的按钮用了一张图，这张图被打进了 `textures/icons` 包
3. 打包器发现：「要实例化登录界面，得先有那张图」
4. 于是自动写入：`ui/login` 依赖 `textures/icons`

### 依赖记在两处（内容一致，用途不同）

```text
ui/login.manifest  →  Dependencies 字段（单包视角）
AssetBundles.manifest →  ui/login 的 Dependencies（全局视角）
```

### 直接依赖 vs 全部依赖

| 概念 | 含义 | 例子 |
|------|------|------|
| 直接依赖 | 只隔一层 | `ui/login` 直接依赖 `textures/icons` |
| 全部依赖 | 含传递链 | 若 A→B→C，加载 A 时 `GetAllDependencies` 会给出 B 和 C |

---

## 八、可选：自定义 JSON 清单（很多项目会加）

Unity 自带的 Manifest 管的是**包与包**的关系。  
业务代码往往还想有一张「**资源路径 → 包名 → 包内名**」的对照表，于是会额外生成 JSON，例如 `AssetCatalog.json`。

下面是一份**完全虚构**的结构，各项目字段可能略有增减：

```json
{
  "version": "1.0.0",
  "buildNumber": 100,
  "platform": "Android",
  "bundleRoot": "/path/to/AssetBundles",
  "entries": [
    {
      "assetPath": "Assets/Prefabs/UI/LoginPanel.prefab",
      "bundleName": "ui/login",
      "assetName": "LoginPanel"
    },
    {
      "assetPath": "Assets/Textures/Icons/Coin.png",
      "bundleName": "textures/icons",
      "assetName": "Coin"
    }
  ]
}
```

### 常见字段

| 字段 | 作用 | 通俗说法 |
|------|------|----------|
| `version` | 资源版本号 | 热更时用来比对「要不要更新」 |
| `buildNumber` | 构建序号 | 比 `version` 更细的整数版本 |
| `platform` | 目标平台 | Android / iOS / Windows 等 |
| `bundleRoot` | 本次 AB 输出根路径 | 这批货放在哪个仓库 |
| `entries` | 资源映射表 | 一本「查号簿」 |
| `entries[].assetPath` | 工程内原始路径 | 开发时查资源、模拟加载用 |
| `entries[].bundleName` | 所属 AB 包名 | 去哪个箱子找 |
| `entries[].assetName` | 包内加载名 | 在箱子里叫啥名 |

### 和 Unity Manifest 的分工

| 问题 | 问谁 |
|------|------|
| `LoginPanel` 在哪个包？ | JSON 清单的 `entries` |
| 加载 `ui/login` 前要先加载谁？ | Unity 的 `AssetBundleManifest` |
| 这个包里到底有哪些工程资源？ | 单包 `.manifest` 的 `Assets` |

注意：**标准 JSON 清单通常不记录包间依赖**；依赖仍以 Unity Manifest 为准。

---

## 九、从查表到进场景的完整流程（虚构例子）

需求：运行时显示登录界面。

```text
① 查自定义清单（若有）
   assetPath 或 业务 key → bundleName = ui/login, assetName = LoginPanel

② 查 Unity 主 Manifest
   GetAllDependencies("ui/login") → ["textures/icons"]

③ 按顺序 LoadFromFile
   textures/icons  →  ui/login

④ LoadAsset
   loginBundle.LoadAsset("LoginPanel") → 得到 Prefab

⑤ Instantiate
   实例化到场景

⑥ 不用时
   减引用 → 引用归零 → Unload 对应包
```

漏掉第 ③ 步的典型后果：界面能出来，但贴图丢失、按钮发白或变粉红。

---

## 十、快速对照：我该看哪个文件？

| 你想知道… | 看哪里 |
|-----------|--------|
| 磁盘上有哪些包 | 输出目录里的包文件；或主 Manifest 的 `AssetBundleInfos` |
| 某个包里有哪些资源 | 该包的 `xxx.manifest` → `Assets` |
| 加载某包前要先加载谁 | 该包 manifest 的 `Dependencies`；或主 Manifest |
| 业务只给资源路径，怎么加载 | 自定义 `AssetCatalog.json`（若有） |
| 包有没有被改过 | `CRC`、`Hashes` |
| 能不能跳过 Manifest | 仅当确认**没有任何跨包依赖**时 |

---

## 十一、和本目录其他文档的关系

| 文档 | 侧重 |
|------|------|
| 本文 | 打包**产物长什么样、字段什么意思** |
| `Assets/vFramework/Docs/AssetBundleGuide.md` | 从打标签到加载卸载的**完整操作流程** |
| `Assets/vFramework/BaseLayer/AssetLayer/ApproachComparisonAndLearningGuide.md` | 多套资源框架的**学习路线与架构对比** |

建议阅读顺序：**本文（认清文件）→ AssetBundleGuide（动手做一遍）→ 方案对比（选型与扩展）**。

---

## 十二、一句话记住

- **`.bundle`** = 装资源的箱子（二进制）
- **`.manifest`** = 箱子的说明书（文本，含装箱清单和前置箱子列表）
- **主 Manifest** = 整个仓库的依赖总表（运行时查依赖靠它）
- **自定义 JSON 清单** = 业务查号簿（资源路径 → 包名 → 包内名），一般不替代 Unity 的依赖信息
