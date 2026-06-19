namespace BaseLayer.Scene
{
    /// <summary>场景加载模式（对应 Unity LoadSceneMode）。</summary>
    public enum SceneLoadMode
    {
        Single = 0,
        Additive = 1
    }

    /// <summary>场景资源来源。</summary>
    public enum SceneSource
    {
        BuildIn = 0,
        AssetBundle = 1
    }

    /// <summary>离开场景时的资源清理策略。</summary>
    public enum SceneCleanupPolicy
    {
        /// <summary>Single 切换：UnloadAll + 池/事件清理。</summary>
        FullUnloadAll = 0,

        /// <summary>Additive / 局部卸载：不 UnloadAll。</summary>
        SceneLocalOnly = 1
    }

    /// <summary>场景切换阶段（供 UI / 日志订阅）。</summary>
    public enum SceneTransitionPhase
    {
        Idle = 0,
        BeforeLeave = 1,
        Loading = 2,
        AfterEnter = 3,
        Completed = 4,
        Failed = 5,
        Cancelled = 6
    }

    /// <summary>新请求与进行中操作冲突时的策略。</summary>
    public enum SceneRequestConflictPolicy
    {
        /// <summary>取消尚未开始的 pending 请求，立即执行新请求。</summary>
        ReplacePending = 0,

        /// <summary>新请求排队，当前完成后执行。</summary>
        Queue = 1
    }
}
