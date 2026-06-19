using System.Collections.Generic;

namespace BaseLayer.Scene.Impt
{
    /// <summary>按 Order 排序执行 <see cref="ISceneTransitionHook"/>。</summary>
    public sealed class SceneCleanupPipeline
    {
        readonly List<ISceneTransitionHook> _hooks;

        public SceneCleanupPipeline(IEnumerable<ISceneTransitionHook> hooks)
        {
            _hooks = new List<ISceneTransitionHook>(hooks ?? System.Array.Empty<ISceneTransitionHook>());
            _hooks.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void OnBeforeLeave(SceneTransitionContext context)
        {
            for (int i = 0; i < _hooks.Count; i++)
                _hooks[i].OnBeforeLeave(context);
        }

        public void OnAfterEnter(SceneTransitionContext context)
        {
            for (int i = 0; i < _hooks.Count; i++)
                _hooks[i].OnAfterEnter(context);
        }
    }
}
