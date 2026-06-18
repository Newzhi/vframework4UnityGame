namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// 热更 / 逻辑层启动装配模板：集中注册 Service 与 Module。
    /// <para>
    /// 接入方式：热更 DLL 加载完成后调用 <see cref="HotUpdateGameEntry.OnHotfixLoaded"/>，
    /// 内部会执行 <see cref="GameRoot.TryStart"/> → 本类 <see cref="Configure"/> → 各 Module.Init。
    /// </para>
    /// <para>
    /// 模块注册规则见 <c>BaseGameRoot/README.md</c> §4；Priority 常量见 <see cref="ModulePriority"/>。
    /// </para>
    /// </summary>
    public sealed class GameBootstrap : IGameBootstrap
    {
        /// <inheritdoc />
        /// <remarks>
        /// 【本方法只做两件事】
        /// 1. services.Register&lt;TInterface&gt;(instance) — 注册可被 IoC / Module.Init 注入的服务
        /// 2. modules.AddModule(new XxxModule()) — 注册参与 Update 生命周期的模块
        ///
        /// 【禁止】
        /// - 在此做耗时 Load、读表、Instantiate（应放到对应 Module.Init 或 GameFlow Boot 态）
        /// - Init 之后 AddModule（ModuleManager 会抛 InvalidOperationException）
        ///
        /// 【Init 执行顺序】
        /// ModuleManager.InitAll 按 IGameModule.Priority 升序调用 Init（数值越小越先 Init）。
        /// </remarks>
        public void Configure(IServiceRegistry services, IModuleRegistry modules)
        {
            // -----------------------------------------------------------------
            // 一、Service 注册（Configure 阶段）
            // -----------------------------------------------------------------
            // 若 Module 本身 implements 某 Service 接口，可先 new 再 Register，再 AddModule 同一实例：
            //
            // var input = new BaseLayer.Input.InputModule();
            // services.Register<BaseLayer.Input.IInputService>(input);
            // modules.AddModule(input);

            // 纯 Service（无 Module 相位）示例：
            // services.Register<IMyGameplayService>(new MyGameplayService());

            // -----------------------------------------------------------------
            // 二、Module 注册（建议按 Priority 从低到高排列，便于阅读）
            // -----------------------------------------------------------------

            // [100] 游戏时间 / Timer / UpdatePipeline（框架内置，建议保留）
            modules.AddModule(new GameTimeModule(new GameTimeOptions
            {
                CalendarSettings = new GameCalendarSettings { SecondsPerDay = 120f },
                InitialTimeScale = 1f
            }));

            // [120] 配置表（BaseConfigSys 落地后取消注释）
            // 职责：GameRoot 已 EnsureReady → 预加载 config AB bytes → GameConfigTables.Initialize
            // modules.AddModule(new BaseFramework.BaseConfigSys.ConfigTableModule());

            // [0] 输入（需要时取消注释）
            // modules.AddModule(new BaseLayer.Input.InputModule());

            // [150] 宏观流程 FSM：Boot → MainMenu → …（MVP 含占位 BootFlowState）
            modules.AddModule(GameFlowModule.CreateMvp(extra: reg =>
            {
                // 在此 Register 额外 IGameFlowState，例如：
                // reg.Register(new BattleFlowState());
            }));

            // [500] 存档
            // modules.AddModule(new BaseLayer.Archive.ArchiveModule(
            //     collector: null,   // 热更层实现 ISaveDataCollector
            //     applier: null));   // 热更层实现 ISaveDataApplier

            // [600] 核心玩法 / ECS / 战斗
            // modules.AddModule(new MyGameLogicModule());

            // [900] 调试命令（开发包）
            // modules.AddModule(new BaseFramework.BaseCommandSys.DebugCommandModule(reg =>
            // {
            //     GameFlowModule.RegisterDebugCommands(reg);
            // }));

            // [1000] UI
            // modules.AddModule(new MyUiModule());

            // -----------------------------------------------------------------
            // 三、自定义 Module 模板（复制到新文件实现）
            // -----------------------------------------------------------------
            // public sealed class MyModule : IGameModule
            // {
            //     public int Priority => 120; // 插在 Early(100) 与 GameFlow(150) 之间
            //     public void Init(IServiceRegistry services) { ... }
            //     public void Update(float deltaTime) { }
            //     public void Dispose() { }
            // }
        }
    }
}
