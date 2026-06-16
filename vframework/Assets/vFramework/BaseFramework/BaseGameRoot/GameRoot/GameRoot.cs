using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏全局唯一入口 MonoBehaviour：装配 IOC、注册模块、驱动 Update / FixedUpdate / LateUpdate。
    /// Bootstrap Scene 中只保留一个实例；必须挂载实现 <see cref="IGameBootstrap"/> 的组件。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [Tooltip("必填：挂载实现 IGameBootstrap 的 MonoBehaviour（业务装配入口）。")]
        [SerializeField] private MonoBehaviour bootstrapBehaviour;

        private ServiceContainer _services;
        private ModuleManager _modules;

        /// <summary>只读服务表，供调试或高级扩展。</summary>
        public IServiceRegistry Services => _services;

        /// <summary>只读模块管理器。</summary>
        public ModuleManager ModuleManager => _modules;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!TryResolveBootstrap(out IGameBootstrap bootstrap))
                return;

            _services = new ServiceContainer();
            _modules = new ModuleManager();
            IoC.SetContainer(_services);

            _modules.Configure(bootstrap, _services);
            _modules.InitAll(_services);
        }

        private void Update()
        {
            _modules?.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _modules?.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            _modules?.LateUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _modules?.DisposeAll();
            _services?.Clear();
            IoC.SetContainer(null);

            if (Instance == this)
                Instance = null;
        }

        private bool TryResolveBootstrap(out IGameBootstrap bootstrap)
        {
            bootstrap = null;

            if (bootstrapBehaviour == null)
            {
                Debug.LogError(
                    $"{nameof(GameRoot)}: Bootstrap Behaviour is required. Assign a {nameof(MonoBehaviour)} that implements {nameof(IGameBootstrap)}.",
                    this);
                enabled = false;
                return false;
            }

            if (bootstrapBehaviour is IGameBootstrap resolved)
            {
                bootstrap = resolved;
                return true;
            }

            Debug.LogError(
                $"{nameof(GameRoot)}: Bootstrap Behaviour must implement {nameof(IGameBootstrap)}. Type: {bootstrapBehaviour.GetType().Name}",
                this);
            enabled = false;
            return false;
        }
    }
}
