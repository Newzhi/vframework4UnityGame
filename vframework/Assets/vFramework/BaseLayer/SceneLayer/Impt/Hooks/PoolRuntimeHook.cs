using UnityEngine;

namespace BaseLayer.Scene.Impt.Hooks
{
    /// <summary>销毁残留 PoolRuntime 根并清 Transform 缓存。</summary>
    public sealed class PoolRuntimeHook : ISceneTransitionHook
    {
        public int Order => 10;

        public void OnBeforeLeave(SceneTransitionContext context)
        {
            if (context.CleanupPolicy != SceneCleanupPolicy.FullUnloadAll)
                return;

            DestroyPoolRuntimeIfExists();
        }

        public void OnAfterEnter(SceneTransitionContext context)
        {
        }

        static void DestroyPoolRuntimeIfExists()
        {
            GameObject poolRuntime = GameObject.Find(PoolSceneRootsUtil.RuntimeRootName);
            if (poolRuntime != null)
                Object.Destroy(poolRuntime);

            PoolSceneRootsUtil.ClearCache();
        }
    }
}
