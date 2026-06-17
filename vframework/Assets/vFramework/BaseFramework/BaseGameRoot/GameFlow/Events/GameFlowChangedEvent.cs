using BaseFramework.BaseEventSys;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 宏观流程切换时发布；UI、存档策略、埋点等可订阅。
    /// </summary>
    public readonly struct GameFlowChangedEvent : IGameEvent
    {
        public string FromStateId { get; }
        public string ToStateId { get; }
        public object UserData { get; }

        public GameFlowChangedEvent(string fromStateId, string toStateId, object userData)
        {
            FromStateId = fromStateId;
            ToStateId = toStateId;
            UserData = userData;
        }
    }
}
