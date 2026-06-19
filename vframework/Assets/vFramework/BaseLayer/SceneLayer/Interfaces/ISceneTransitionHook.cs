namespace BaseLayer.Scene
{
    /// <summary>场景切换生命周期钩子上下文。</summary>
    public sealed class SceneTransitionContext
    {
        public string FromSceneId { get; set; }
        public string ToSceneId { get; set; }
        public SceneLoadMode Mode { get; set; }
        public SceneCleanupPolicy CleanupPolicy { get; set; }
        public object UserData { get; set; }
    }

    /// <summary>场景切换前后扩展点；Order 越小越先执行。</summary>
    public interface ISceneTransitionHook
    {
        int Order { get; }
        void OnBeforeLeave(SceneTransitionContext context);
        void OnAfterEnter(SceneTransitionContext context);
    }
}
