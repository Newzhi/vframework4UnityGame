namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏宏观流程运行时门面：查询当前/上一状态、切换阶段、读取进入时长。
    /// 由 <see cref="GameFlowModule"/> 注册到 IOC；具体状态类在热更层 Register。
    /// </summary>
    public interface IGameFlowService
    {
        /// <summary>当前状态 Id；尚未成功 <see cref="ChangeState"/> 时为 null。</summary>
        string CurrentStateId { get; }

        /// <summary>上一次所在状态 Id；首次进入流程前为 null。</summary>
        string PreviousStateId { get; }

        /// <summary>
        /// 当前状态已持续的秒数（基于 Time.realtimeSinceStartup，不受 TimeScale 影响）。
        /// 用于调试、超时判定；存档语义请用 GameTime / Collector 自行定义。
        /// </summary>
        float CurrentStateElapsedSeconds { get; }

        /// <summary>是否处于指定 stateId（Ordinal 字符串比较）。</summary>
        bool IsInState(string stateId);

        /// <summary>
        /// 切换到已注册状态：先 Exit 旧态 → 更新 Previous → Enter 新态 → 发布 <see cref="GameFlowChangedEvent"/>。
        /// 目标 Id 与当前相同则为 no-op（不重复 Enter）。
        /// </summary>
        /// <param name="stateId">已在 Registry 注册的 Id。</param>
        /// <param name="userData">经 <see cref="IGameFlowContext.UserData"/> 传给新态 Enter（如 sceneName）。</param>
        void ChangeState(string stateId, object userData = null);
    }
}
