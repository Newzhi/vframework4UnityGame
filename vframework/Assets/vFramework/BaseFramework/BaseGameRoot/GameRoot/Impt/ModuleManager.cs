using System.Collections.Generic;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 模块生命周期与 Update 调度（Update / FixedUpdate / LateUpdate / Editor Gizmo）。
    /// </summary>
    public sealed class ModuleManager : IModuleRegistry
    {
        private readonly List<IGameModule> _modules = new List<IGameModule>(8);
        private readonly List<IFixedUpdateModule> _fixedUpdateModules = new List<IFixedUpdateModule>(4);
        private readonly List<ILateUpdateModule> _lateUpdateModules = new List<ILateUpdateModule>(4);
        private readonly List<IGizmoDrawModule> _gizmoDrawModules = new List<IGizmoDrawModule>(4);
        private readonly List<IGizmoDrawSelectedModule> _gizmoDrawSelectedModules = new List<IGizmoDrawSelectedModule>(4);
        private bool _initialized;

        public IReadOnlyList<IGameModule> Modules => _modules;

        public void Configure(IGameBootstrap bootstrap, IServiceRegistry services)
        {
            if (bootstrap == null)
                throw new System.ArgumentNullException(nameof(bootstrap));

            bootstrap.Configure(services, this);
        }

        public void AddModule(IGameModule module)
        {
            if (module == null)
                throw new System.ArgumentNullException(nameof(module));

            if (_initialized)
                throw new System.InvalidOperationException("Cannot add module after InitAll.");

            _modules.Add(module);
        }

        public void InitAll(IServiceRegistry services)
        {
            _modules.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));
            _fixedUpdateModules.Clear();
            _lateUpdateModules.Clear();
            _gizmoDrawModules.Clear();
            _gizmoDrawSelectedModules.Clear();

            for (int i = 0; i < _modules.Count; i++)
            {
                IGameModule module = _modules[i];
                module.Init(services);

                if (module is IFixedUpdateModule fixedUpdate)
                    _fixedUpdateModules.Add(fixedUpdate);

                if (module is ILateUpdateModule lateUpdate)
                    _lateUpdateModules.Add(lateUpdate);

                if (module is IGizmoDrawModule gizmoDraw)
                    _gizmoDrawModules.Add(gizmoDraw);

                if (module is IGizmoDrawSelectedModule gizmoSelected)
                    _gizmoDrawSelectedModules.Add(gizmoSelected);
            }

            _initialized = true;
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < _modules.Count; i++)
                _modules[i].Update(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            for (int i = 0; i < _fixedUpdateModules.Count; i++)
                _fixedUpdateModules[i].FixedUpdate(fixedDeltaTime);
        }

        public void LateUpdate(float deltaTime)
        {
            for (int i = 0; i < _lateUpdateModules.Count; i++)
                _lateUpdateModules[i].LateUpdate(deltaTime);
        }

        /// <summary>
        /// Editor Scene 视图 Gizmo；由 <see cref="GameRoot.OnDrawGizmos"/> 调用。
        /// 顺序与 <see cref="Priority"/> 一致。
        /// </summary>
        public void DrawGizmos()
        {
            for (int i = 0; i < _gizmoDrawModules.Count; i++)
                _gizmoDrawModules[i].DrawGizmos();
        }

        /// <summary>
        /// Editor Scene 视图 Gizmo（仅 GameRoot 被选中）；由 <see cref="GameRoot.OnDrawGizmosSelected"/> 调用。
        /// </summary>
        public void DrawGizmosSelected()
        {
            for (int i = 0; i < _gizmoDrawSelectedModules.Count; i++)
                _gizmoDrawSelectedModules[i].DrawGizmosSelected();
        }

        public void DisposeAll()
        {
            for (int i = _modules.Count - 1; i >= 0; i--)
                _modules[i].Dispose();

            _modules.Clear();
            _fixedUpdateModules.Clear();
            _lateUpdateModules.Clear();
            _gizmoDrawModules.Clear();
            _gizmoDrawSelectedModules.Clear();
            _initialized = false;
        }
    }
}
