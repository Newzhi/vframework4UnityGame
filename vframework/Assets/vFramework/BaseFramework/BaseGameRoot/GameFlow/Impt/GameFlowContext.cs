namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IGameFlowContext"/> 默认实现。
    /// <see cref="UserData"/> 在每次 <see cref="GameFlowService.ChangeState"/> 时由 Service 写入。
    /// </summary>
    internal sealed class GameFlowContext : IGameFlowContext
    {
        /// <summary>绑定 IOC 与流程服务，生命周期与 GameFlowService 相同。</summary>
        public GameFlowContext(IServiceRegistry services, IGameFlowService flow)
        {
            Services = services;
            Flow = flow;
        }

        /// <inheritdoc />
        public IServiceRegistry Services { get; }

        /// <inheritdoc />
        public IGameFlowService Flow { get; }

        /// <inheritdoc />
        public object UserData { get; internal set; }
    }
}
