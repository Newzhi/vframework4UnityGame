namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 配置阶段注册流程状态；仅在 <see cref="GameFlowModule.Init"/> 回调中使用。
    /// </summary>
    public interface IGameFlowRegistry
    {
        /// <summary>注册一个状态；重复 Id 抛 <see cref="System.ArgumentException"/>。</summary>
        void Register(IGameFlowState state);

        /// <summary>是否已注册指定 Id。</summary>
        bool Contains(string stateId);
    }
}
