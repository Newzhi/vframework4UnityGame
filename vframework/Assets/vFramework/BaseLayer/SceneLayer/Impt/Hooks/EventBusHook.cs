using BaseFramework.BaseEventSys;

namespace BaseLayer.Scene.Impt.Hooks
{
    /// <summary>Single 切换前清空全局事件订阅。</summary>
    public sealed class EventBusHook : ISceneTransitionHook
    {
        public int Order => 20;

        public void OnBeforeLeave(SceneTransitionContext context)
        {
            if (context.CleanupPolicy != SceneCleanupPolicy.FullUnloadAll)
                return;

            GameEventBus.ClearAll();
        }

        public void OnAfterEnter(SceneTransitionContext context)
        {
        }
    }
}
