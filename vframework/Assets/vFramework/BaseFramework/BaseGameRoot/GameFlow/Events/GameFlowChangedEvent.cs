using BaseFramework.BaseEventSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 宏观流程切换成功后发布（经 <see cref="GameEventBus.SentEvent"/>）。
    /// UI、存档策略、Analytics 等可订阅；勿用于每帧高频逻辑。
    /// </summary>
    public readonly struct GameFlowChangedEvent : IGameEvent
    {
        /// <summary>离开的状态 Id；首次进入流程前为 null。</summary>
        public string FromStateId { get; }

        /// <summary>进入的状态 Id。</summary>
        public string ToStateId { get; }

        /// <summary>本次 ChangeState 传入的 userData 引用（只读传递，勿长期持有 UnityEngine.Object）。</summary>
        public object UserData { get; }

        public GameFlowChangedEvent(string fromStateId, string toStateId, object userData)
        {
            FromStateId = fromStateId;
            ToStateId = toStateId;
            UserData = userData;
        }
    }
}
