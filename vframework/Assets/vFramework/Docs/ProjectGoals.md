# 项目目标

本文档描述 **vFramework** 的定位、能力范围与阶段性目标。框架面向 **通用、简单的 Unity 游戏项目**，不绑定具体品类；用小型 Demo 或样板场景验证模块即可。

> 练习项目性质见仓库根目录 [README.md](../../../../README.md) 免责声明。

---

## 1. 框架定位

**通用轻量游戏框架（General Lightweight Game Framework for Unity）**

为常见中小型游戏提供可复用骨架：启动与模块调度、资源加载、UI/音频/输入等全局系统、可选的热更与联机扩展。业务玩法（RPG、休闲、对战、解谜等）由 **HotUpdateLayer** 自行实现，框架层不写死品类逻辑。

框架需同时支撑：

- **单机 / 本地调试**：快速搭场景、验模块、跑集成测试。
- **可选联机**：房间、状态同步、断线重连等由 NetMgr + Proxy 扩展，非所有项目必选。
- **持续迭代**：资源与逻辑热更、配置表驱动，便于内容扩展与平衡调整。

---

## 2. 典型需求与框架能力（对照）

| 常见需求 | 对框架的要求 |
|----------|----------------|
| **多实体同屏**（角色、子弹、特效） | 对象池（`ObjPoolMgr`）、异步加载、性能友好的 Update 管线 |
| **场景与流程切换** | 宏观 `GameFlow`（Boot / 主菜单 / 局内）、`SceneMgr`；局内细粒度可用嵌套 FSM |
| **玩法规则与输入** | 逻辑层 MVC + Proxy，Model 存状态，Controller 处理输入与规则 |
| **多人状态一致**（若需要） | 网络层收发协议，Proxy 解析并更新 Model，再通知 Controller / View |
| **UI 较多** | `UIMgr`、View 与 Model 解耦，事件总线辅助刷新 |
| **配置驱动** | 配置表模块、资源热更、逻辑热更 |

---

## 3. 技术目标

### 3.1 框架层（vFramework）

- 提供清晰的三层结构：**基础架构层 → 全局系统层 → 业务逻辑层**。
- **BaseGameRoot**（生命周期 + IOC）与 **BaseAssetSys**（资源加载）为同级独立子系统，文档与排期分开维护。
- 全局 Manager 由 **GameRoot** 注册与 Tick，生命周期可控（不替代 Bundle Load API）。
- **资源域**（`BaseAssetSys` / `AssetLayer`）统一管理资源加载、场景、对象池。
- **第三方依赖下沉**：UniTask、Addressables 等仅在底层引用，上层通过接口访问。
- 业务层采用 **MVC + Proxy**：需要联机时由 Proxy 承接网络数据并更新 Model，Controller 协调玩法与 View。

### 3.2 联机相关（可选）

- 客户端-服务器（或 Host）架构，协议与传输在 **BaseNetwork / NetMgr** 层隔离。
- 业务关键状态（实体 ID、房间状态、分数等）需可序列化、可同步、可回放排查。
- 支持登录 → 大厅/匹配 → 加载场景 → 局内 → 结算等链路的扩展点。

### 3.3 工程与交付

- 程序集（`asmdef`）按层拆分，热更程序集 `HotUpdateLayer` 承载主要业务。
- 文档与代码目录一致，模块职责可单独测试与替换。
- 优先完成 **P0 骨架**（BaseGameRoot、EventBus、BaseAssetSys、网络分发、Logic Bootstrap），再按项目扩展玩法。

---

## 4. 阶段性范围

### 当前阶段（框架搭建）

- [ ] **BaseGameRoot**：GameRoot 与模块注册中心（与资源加载独立）
- [ ] BaseEventSys 事件总线
- [ ] **BaseAssetSys**：`ResMgr` / `SceneMgr` / `ObjPoolMgr` 或现有 Bundle 加载 API（排期见 BaseAssetSys MainRoadmap）
- [ ] 网络底层与消息分发
- [ ] HotUpdateLayer：AppContext、Proxy / Controller 骨架
- [ ] 可运行的样板 / 综合测试场景（验证框架，非完整产品）

### 后续阶段（按项目选用）

- 完整匹配与房间系统
- 权威服务器或帧同步方案选型与落地
- 玩法配置管线（数值表、关卡表等）
- 商业化与运营相关模块（按需）

---

## 5. 非目标（现阶段不做）

- 不做大而全的 MMO 全套（公会、大世界等）除非业务单独立项。
- 不在 BaseFramework 中编写具体玩法业务逻辑（敌人 AI、关卡规则等）。
- 不追求一次到位的万能框架；先保证分层清晰、可替换、易上手。

---

## 6. 成功标准

1. **分层清晰**：业务代码不直接引用 Addressables / Socket 等第三方 API。
2. **联机可扩展**（若需要）：新增一条协议只需增加 Proxy + Model，不改 NetMgr 核心。
3. **可验证**：能在本地（或联机）模式下完成「启动 → 进菜单 → 进局内 → 结算」类最小闭环 Demo。
4. **团队可协作**：程序、策划、UI 可依据目录与文档并行开发。

---

## 相关文档

- [框架设计构思](./FrameworkDesign.md)
