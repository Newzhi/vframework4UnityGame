using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏全局唯一入口 MonoBehaviour：装配 IOC、注册模块、驱动 Update / FixedUpdate / LateUpdate。
    /// Bootstrap Scene 中只保留一个实例。业务装配在热更加载后调用 <see cref="TryStart"/>（路径 B）。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        private ServiceContainer _services;
        private ModuleManager _modules;
        private bool _started;
        private bool _waitingBootstrap;

        public bool IsStarted => _started;
        public IServiceRegistry Services => _services;
        public ModuleManager ModuleManager => _modules;

        public static bool TryStart(IGameBootstrap bootstrap)
        {
            if (bootstrap == null)
            {
                Debug.LogError($"{nameof(GameRoot)}.{nameof(TryStart)}: bootstrap is null.");
                return false;
            }

            GameBootstrapRegistry.Register(bootstrap);

            if (Instance == null)
                return true;

            if (Instance._started)
            {
                Debug.LogError($"{nameof(GameRoot)}.{nameof(TryStart)}: pipeline already started.");
                return false;
            }

            Instance.StartPipeline(bootstrap);
            return true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GameBootstrapRegistry.TryGet(out IGameBootstrap bootstrap))
                StartPipeline(bootstrap);
            else
                _waitingBootstrap = true;
        }

        private void Start()
        {
            if (_waitingBootstrap && !_started)
            {
                Debug.LogError(
                    $"{nameof(GameRoot)}: Bootstrap not started. Call {nameof(TryStart)}({nameof(IGameBootstrap)}) after hotfix / logic assembly is loaded.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_started) return;
            _modules.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!_started) return;
            _modules.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (!_started) return;
            _modules.LateUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_started)
            {
                _modules?.DisposeAll();
                _services?.Clear();
                IoC.SetContainer(null);
            }

            GameBootstrapRegistry.Clear();

            if (Instance == this)
                Instance = null;
        }

        private void StartPipeline(IGameBootstrap bootstrap)
        {
            if (_started || bootstrap == null)
                return;

            _services = new ServiceContainer();
            _modules = new ModuleManager();
            IoC.SetContainer(_services);

            _modules.Configure(bootstrap, _services);
            _modules.InitAll(_services);

            _started = true;
            _waitingBootstrap = false;
        }
    }
}
