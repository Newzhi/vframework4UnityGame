namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 配置阶段注册流程状态；仅在 <see cref="GameFlowModule"/> 的 Init → registerStates 回调中使用。
    /// 运行时切换请用 <see cref="IGameFlowService"/>。
    /// </summary>
    public interface IGameFlowRegistry
    {
        /// <summary>注册一个状态实例；state.Id 重复时抛 <see cref="System.ArgumentException"/>。</summary>
        void Register(IGameFlowState state);

        /// <summary>是否已注册指定 Id（Bootstrap 可选校验）。</summary>
        bool Contains(string stateId);
    }
}
