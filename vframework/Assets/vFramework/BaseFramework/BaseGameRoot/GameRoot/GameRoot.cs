using UnityEngine;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏全局唯一入口 MonoBehaviour：装配 IOC、注册模块、驱动 Update / FixedUpdate / LateUpdate；
    /// Editor 下另转发 Scene Gizmo（<see cref="IGizmoDrawModule"/>）。
    /// Bootstrap Scene 中只保留一个实例。业务装配在热更加载后调用 <see cref="TryStart"/>（路径 B）。
    /// <see cref="StartPipeline"/> 会先 <see cref="EnsureAssetSystemReady"/>，再 Configure / InitAll（配置表等 Module 在其后）。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [Header("Asset System")]
        [Tooltip("空=默认 StreamingAssets/{平台}/；可填 persistentDataPath 下 AB 缓存根等")]
        [SerializeField] string bundleRootOverride;

        [Tooltip("bundleRootOverride 为空时仍对默认 StreamingAssets 根追加平台子目录")]
        [SerializeField] bool usePlatformSubfolder = true;

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

            return Instance.StartPipeline(bootstrap);
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

#if UNITY_EDITOR
        /// <summary>
        /// Editor Scene 视图：转发至实现了 <see cref="IGizmoDrawModule"/> 的模块。
        /// 未 TryStart（<see cref="_started"/> 为 false）时不绘制，避免访问未 Init 的 Module。
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_started || _modules == null)
                return;

            _modules.DrawGizmos();
        }

        /// <summary>
        /// Editor Scene 视图：Hierarchy 选中本 GameObject 时，转发 <see cref="IGizmoDrawSelectedModule"/>。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_started || _modules == null)
                return;

            _modules.DrawGizmosSelected();
        }
#endif

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

        private bool StartPipeline(IGameBootstrap bootstrap)
        {
            if (_started || bootstrap == null)
                return _started;

            if (!EnsureAssetSystemReady())
            {
                Debug.LogError($"{nameof(GameRoot)}: Asset system init failed.", this);
                enabled = false;
                return false;
            }

            _services = new ServiceContainer();
            _modules = new ModuleManager();
            IoC.SetContainer(_services);

            _modules.Configure(bootstrap, _services);
            _modules.InitAll(_services);

            if (_services.TryGet(out IGameUpdatePipeline pipeline))
                _updatePipeline = pipeline;

            _started = true;
            _waitingBootstrap = false;
            return true;
        }

        /// <summary>
        /// 预热 <see cref="BundleResLoader"/>：读 catalog.bytes，初始化 BundleManager / AssetRouter。
        /// 必须在 Module InitAll 之前完成，以便后续配置表 Module 可直接 Load。
        /// </summary>
        bool EnsureAssetSystemReady()
        {
            if (string.IsNullOrEmpty(bundleRootOverride))
                return BundleResLoader.Instance.EnsureReady();

            return BundleResLoader.Instance.Init(bundleRootOverride, usePlatformSubfolder);
        }
    }
}
