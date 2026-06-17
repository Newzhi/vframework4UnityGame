using System;
using BaseFramework.BaseCommandSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏宏观流程模块：注册 <see cref="IGameFlowService"/>，由 <see cref="GameRoot"/> 每帧 Tick 当前状态。
    /// <para>
    /// 用法（热更 Bootstrap）：
    /// <code>
    /// modules.AddModule(GameFlowModule.CreateMvp(extra: reg =>
    /// {
    ///     reg.Register(new ProcedureBattle()); // 新增状态：实现 IGameFlowState + Register
    /// }));
    /// </code>
    /// 调试命令（可选，在 DebugCommandModule 中注册）：
    /// <see cref="FlowStateCommand"/>、<see cref="FlowGotoCommand"/>。
    /// </para>
    /// </summary>
    public sealed class GameFlowModule : IGameModule
    {
        private readonly Action<IGameFlowRegistry> _registerStates;
        private readonly string _initialStateId;
        private GameFlowService _service;

        public int Priority => ModulePriority.GameFlow;

        /// <param name="registerStates">配置阶段注册所有 <see cref="IGameFlowState"/>。</param>
        /// <param name="initialStateId">InitAll 完成后切入的首状态；null 表示不自动切换。</param>
        public GameFlowModule(
            Action<IGameFlowRegistry> registerStates = null,
            string initialStateId = null)
        {
            _registerStates = registerStates;
            _initialStateId = initialStateId;
        }

        /// <summary>
        /// 内置 MVP：Boot + MainMenu，启动后自动进入 Boot。
        /// 扩展时在 <paramref name="extra"/> 里 Register 更多状态，并修改 Boot 内的切换逻辑。
        /// </summary>
        public static GameFlowModule CreateMvp(Action<IGameFlowRegistry> extra = null)
        {
            return new GameFlowModule(
                registerStates: reg =>
                {
                    reg.Register(new BootFlowState());
                    reg.Register(new MainMenuFlowState());
                    extra?.Invoke(reg);
                },
                initialStateId: GameFlowIds.Boot);
        }

        /// <summary>
        /// 向 <see cref="BaseCommandSys.DebugCommandModule"/> 的 registerExtra 注册 flow.state / flow.goto。
        /// </summary>
        public static void RegisterDebugCommands(ICommandRegistry registry)
        {
            if (registry == null)
                return;

            registry.Register(new FlowStateCommand());
            registry.Register(new FlowGotoCommand());
        }

        /// <inheritdoc />
        public void Init(IServiceRegistry services)
        {
            _service = new GameFlowService(services);
            _registerStates?.Invoke(_service);
            services.Register<IGameFlowService>(_service);

            if (!string.IsNullOrEmpty(_initialStateId))
                _service.ChangeState(_initialStateId);
        }

        /// <inheritdoc />
        public void Update(float deltaTime)
        {
            _service?.Tick(deltaTime);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _service?.Shutdown();
            _service = null;
        }
    }
}
