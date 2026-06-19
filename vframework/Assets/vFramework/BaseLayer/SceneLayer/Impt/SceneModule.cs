using System.Collections.Generic;
using BaseFramework.BaseGameRoot;
using BaseLayer.Scene.Impt.Hooks;

namespace BaseLayer.Scene.Impt
{
    /// <summary>
    /// 场景模块：注册 <see cref="ISceneService"/>，Priority 140（ConfigTable 之后、GameFlow 之前）。
    /// </summary>
    public sealed class SceneModule : IGameModule
    {
        readonly SceneCatalog _catalog;
        readonly IEnumerable<ISceneTransitionHook> _extraHooks;
        SceneService _service;

        public int Priority => ModulePriority.Scene;

        public SceneModule(SceneCatalog catalog, IEnumerable<ISceneTransitionHook> extraHooks = null)
        {
            _catalog = catalog;
            _extraHooks = extraHooks;
        }

        public void Init(IServiceRegistry services)
        {
            _service = new SceneService(_catalog, _extraHooks);
            services.Register<ISceneService>(_service);
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
            _service?.Dispose();
            _service = null;
        }
    }
}
