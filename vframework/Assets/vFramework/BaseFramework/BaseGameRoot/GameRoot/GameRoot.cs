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

        /// <summary>IOC 服务容器；<see cref="StartPipeline"/> 时创建，<see cref="OnDestroy"/> 时清空。</summary>
        private ServiceContainer _services;

        /// <summary>模块调度器：InitAll / Update / FixedUpdate / LateUpdate / DisposeAll。</summary>
        private ModuleManager _modules;

        /// <summary>GameTime 帧管道；注册 <see cref="GameTimeModule"/> 后可用，否则 Update 回退 Unity deltaTime。</summary>
        private IGameUpdatePipeline _updatePipeline;

        /// <summary>是否已完成 Configure + InitAll，三相位 Update 仅在此后为 true。</summary>
        private bool _started;

        /// <summary>Awake 时 Registry 尚无 Bootstrap，等待热更层 <see cref="TryStart"/>。</summary>
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

            if (_updatePipeline != null)
                _updatePipeline.RunFrame(Time.deltaTime, _modules.Update);
            else
                _modules.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!_started) return;

            if (_updatePipeline != null)
                _updatePipeline.RunFixedFrame(Time.fixedDeltaTime, _modules.FixedUpdate);
            else
                _modules.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (!_started) return;

            if (_updatePipeline != null)
                _updatePipeline.RunLateFrame(Time.deltaTime, _modules.LateUpdate);
            else
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

            if (_services.TryGet(out IGameUpdatePipeline pipeline))
                _updatePipeline = pipeline;

            _started = true;
            _waitingBootstrap = false;
        }
    }
}
