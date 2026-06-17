namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏宏观流程：查询当前态、切换状态、记录上一状态。
    /// 由 <see cref="GameFlowModule"/> 注册到 IOC；业务通过 Bootstrap 注入具体 <see cref="IGameFlowState"/>。
    /// </summary>
    public interface IGameFlowService
    {
        /// <summary>当前状态 Id；尚未 <see cref="ChangeState"/> 时为 null。</summary>
        string CurrentStateId { get; }

        /// <summary>上一次状态 Id；首次进入前为 null。</summary>
        string PreviousStateId { get; }

        /// <summary>当前状态已持续的 Unity 实时秒数（不受 TimeScale 影响）。</summary>
        float CurrentStateElapsedSeconds { get; }

        /// <summary>是否处于指定状态。</summary>
        bool IsInState(string stateId);

        /// <summary>
        /// 切换到已注册的状态。同 Id 默认 no-op（不重复 Enter）。
        /// </summary>
        /// <param name="stateId">目标状态 Id。</param>
        /// <param name="userData">传给下一状态 Enter 的可选参数。</param>
        void ChangeState(string stateId, object userData = null);
    }
}
