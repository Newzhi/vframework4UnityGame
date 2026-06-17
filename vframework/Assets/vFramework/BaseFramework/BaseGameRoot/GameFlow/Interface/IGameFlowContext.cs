namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 状态生命周期内可用的只读上下文；由 <see cref="GameFlowService"/> 在切换时注入。
    /// </summary>
    public interface IGameFlowContext
    {
        /// <summary>IOC 容器；Init 阶段依赖应已注册完毕。</summary>
        IServiceRegistry Services { get; }

        /// <summary>当前流程服务；状态内切换请调用 <see cref="IGameFlowService.ChangeState"/>。</summary>
        IGameFlowService Flow { get; }

        /// <summary>本次 <see cref="IGameFlowService.ChangeState"/> 传入的附加参数（如 sceneName、关卡 Id）。</summary>
        object UserData { get; }
    }
}
