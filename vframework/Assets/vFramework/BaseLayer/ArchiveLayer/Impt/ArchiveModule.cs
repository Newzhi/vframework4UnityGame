using BaseFramework.BaseGameRoot;

namespace BaseLayer.Archive
{
    /// <summary>
    /// 存档模块：注册 <see cref="IArchiveService"/>，可选启动自动存档 Timer。
    /// </summary>
    public sealed class ArchiveModule : IGameModule
    {
        private readonly ArchiveService _service;
        private readonly IAutoSavePolicy _autoSavePolicy;
        private readonly float _autoSaveIntervalSeconds;
        private AutoSaveController _autoSave;

        public int Priority => ModulePriority.Normal;

        /// <param name="collector">热更层：收集 payload。</param>
        /// <param name="applier">热更层：读档灌入。</param>
        /// <param name="storage">存储后端；null 使用 <see cref="FileArchiveStorage"/>。</param>
        /// <param name="manualSlotCount">手动槽位数，默认 3。</param>
        /// <param name="autoSaveIntervalSeconds">自动存档间隔（秒）；≤0 表示不启用。</param>
        /// <param name="autoSavePolicy">自动存档策略；null 使用 <see cref="AlwaysAutoSavePolicy"/>。</param>
        public ArchiveModule(
            ISaveDataCollector collector,
            ISaveDataApplier applier,
            IArchiveStorage storage = null,
            int manualSlotCount = 3,
            float autoSaveIntervalSeconds = 120f,
            IAutoSavePolicy autoSavePolicy = null)
        {
            _service = new ArchiveService(collector, applier, storage, manualSlotCount);
            _autoSaveIntervalSeconds = autoSaveIntervalSeconds;
            _autoSavePolicy = autoSavePolicy;
        }

        /// <inheritdoc />
        public void Init(IServiceRegistry services)
        {
            services.Register<IArchiveService>(_service);

            if (services.TryGet(out IGameTimeClock clock))
            {
                services.TryGet(out ISessionTimeline timeline);
                _service.BindGameTime(clock, timeline);
            }

            if (_autoSaveIntervalSeconds > 0f && services.TryGet(out ITimerService timers))
            {
                _autoSave = new AutoSaveController(_service, timers, _autoSavePolicy);
                _autoSave.Start(_autoSaveIntervalSeconds);
            }
        }

        /// <inheritdoc />
        public void Update(float deltaTime)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _autoSave?.Stop();
            _autoSave = null;
        }
    }
}
