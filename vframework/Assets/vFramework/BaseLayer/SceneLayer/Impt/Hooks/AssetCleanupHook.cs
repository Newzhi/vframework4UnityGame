namespace BaseLayer.Scene.Impt.Hooks
{
    /// <summary>Single 切换前 <see cref="BundleResLoader.UnloadAll"/>。</summary>
    public sealed class AssetCleanupHook : ISceneTransitionHook
    {
        public int Order => 0;

        public void OnBeforeLeave(SceneTransitionContext context)
        {
            if (context.CleanupPolicy != SceneCleanupPolicy.FullUnloadAll)
                return;

            SceneBundleLoader.ReleaseAll();
            BundleResLoader.Instance.UnloadAll();
        }

        public void OnAfterEnter(SceneTransitionContext context)
        {
        }
    }
}
