using System;
using BaseFramework.BaseCommandSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏宏观流程模块：注册 <see cref="IGameFlowService"/>，由 <see cref="GameRoot"/> 经
    /// <see cref="ModuleManager"/> 每帧 Tick 当前 <see cref="IGameFlowState"/>。
    /// <para>详见 <c>GameFlow/GameFlowApi.md</c>。</para>
    /// </summary>
    public sealed class GameFlowModule : IGameModule
    {
        /// <summary>Init 时调用，向内部 Service 注册所有流程状态。</summary>
        private readonly Action<IGameFlowRegistry> _registerStates;

        /// <summary>Init 末尾自动 ChangeState 的目标 Id；null 表示不自动切入。</summary>
        private readonly string _initialStateId;

        /// <summary>流程调度器实例；Init 创建，Dispose 置 null。</summary>
        private GameFlowService _service;

        /// <inheritdoc />
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
        /// 内置 MVP：注册 <see cref="BootFlowState"/> + <see cref="MainMenuFlowState"/>，
        /// 启动后自动 <c>ChangeState(Boot)</c>。扩展时在 <paramref name="extra"/> 里 Register 更多状态。
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
        /// 向 <see cref="DebugCommandModule"/> 的 registerExtra 注册 <c>flow.state</c> / <c>flow.goto</c>。
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
