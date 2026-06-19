using BaseFramework.BaseEventSys;

namespace BaseLayer.Scene
{
    /// <summary>
    /// 场景切换阶段变化时发布（经 <see cref="GameEventBus.SentEvent"/>）。
    /// UI、Loading 条等可订阅；勿用于每帧逻辑。
    /// </summary>
    public readonly struct SceneTransitionEvent : IGameEvent
    {
        public SceneTransitionPhase Phase { get; }
        public string FromSceneId { get; }
        public string ToSceneId { get; }
        public float Progress { get; }
        public object UserData { get; }
        public string ErrorMessage { get; }

        public SceneTransitionEvent(
            SceneTransitionPhase phase,
            string fromSceneId,
            string toSceneId,
            float progress = 0f,
            object userData = null,
            string errorMessage = null)
        {
            Phase = phase;
            FromSceneId = fromSceneId;
            ToSceneId = toSceneId;
            Progress = progress;
            UserData = userData;
            ErrorMessage = errorMessage;
        }
    }
}
