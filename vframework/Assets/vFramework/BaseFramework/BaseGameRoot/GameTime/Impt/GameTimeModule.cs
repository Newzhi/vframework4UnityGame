namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 框架内置模块：注册 Clock / Moment / Calendar / Timer / 三相位 Facade / Pipeline。
    /// Priority 为 <see cref="ModulePriority.Early"/>；自身 Update 为空，帧逻辑由 Pipeline 驱动。
    /// </summary>
    public sealed class GameTimeModule : IGameModule
    {
        private readonly GameTimeOptions _options;
        private TimerService _timerService;
        private UpdateFacade _updateFacade;
        private FixedUpdateFacade _fixedUpdateFacade;
        private LateUpdateFacade _lateUpdateFacade;

        /// <inheritdoc />
        public int Priority => ModulePriority.Early;

        /// <summary>可选 Bootstrap 配置；null 时使用默认 Options。</summary>
        public GameTimeModule(GameTimeOptions options = null)
        {
            _options = options ?? new GameTimeOptions();
        }

        /// <inheritdoc />
        public void Init(IServiceRegistry services)
        {
            var clock = new GameTimeClock
            {
                TimeScale = _options.InitialTimeScale
            };
            var timeline = new SessionTimeline();
            var calendar = new GameCalendar();

            if (_options.CalendarSettings != null)
                calendar.Configure(_options.CalendarSettings);

            var momentProvider = new GameMomentProvider(clock, timeline, calendar);
            _timerService = new TimerService();
            _updateFacade = new UpdateFacade();
            _fixedUpdateFacade = new FixedUpdateFacade();
            _lateUpdateFacade = new LateUpdateFacade();

            services.Register<IGameTimeClock>(clock);
            services.Register<ISessionTimeline>(timeline);
            services.Register<IGameCalendar>(calendar);
            services.Register<IGameMomentProvider>(momentProvider);
            services.Register<ITimerService>(_timerService);
            services.Register<IUpdateFacade>(_updateFacade);
            services.Register<IFixedUpdateFacade>(_fixedUpdateFacade);
            services.Register<ILateUpdateFacade>(_lateUpdateFacade);

            var pipeline = new GameUpdatePipeline(
                clock,
                _updateFacade,
                _fixedUpdateFacade,
                _lateUpdateFacade,
                calendar,
                _timerService);
            services.Register<IGameUpdatePipeline>(pipeline);
        }

        /// <inheritdoc />
        public void Update(float deltaTime)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timerService?.CancelAll();
            _updateFacade?.Clear();
            _fixedUpdateFacade?.Clear();
            _lateUpdateFacade?.Clear();
        }
    }
}
