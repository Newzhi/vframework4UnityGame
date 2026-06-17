using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IGameUpdatePipeline"/> 默认实现；持有 GameTime 子系统引用并定义三相位推进顺序。
    /// </summary>
    public sealed class GameUpdatePipeline : IGameUpdatePipeline
    {
        private readonly IGameTimeClock _clock;
        private readonly UpdateFacade _updateFacade;
        private readonly FixedUpdateFacade _fixedUpdateFacade;
        private readonly LateUpdateFacade _lateUpdateFacade;
        private readonly IGameCalendar _calendar;
        private readonly TimerService _timerService;

        /// <inheritdoc />
        public float GameDeltaTime => _clock.DeltaTime;

        public GameUpdatePipeline(
            IGameTimeClock clock,
            UpdateFacade updateFacade,
            FixedUpdateFacade fixedUpdateFacade,
            LateUpdateFacade lateUpdateFacade,
            IGameCalendar calendar,
            TimerService timerService)
        {
            _clock = clock;
            _updateFacade = updateFacade;
            _fixedUpdateFacade = fixedUpdateFacade;
            _lateUpdateFacade = lateUpdateFacade;
            _calendar = calendar;
            _timerService = timerService;
        }

        /// <inheritdoc />
        public void RunFrame(float rawDelta, Action<float> moduleUpdate)
        {
            _clock.Advance(rawDelta);
            float gameDelta = _clock.DeltaTime;

            moduleUpdate?.Invoke(gameDelta);
            _updateFacade.Tick(gameDelta);

            if (_calendar.IsEnabled)
                _calendar.Advance(gameDelta);

            _timerService.Tick(gameDelta);
        }

        /// <inheritdoc />
        public void RunFixedFrame(float rawFixedDelta, Action<float> moduleFixedUpdate)
        {
            moduleFixedUpdate?.Invoke(rawFixedDelta);
            _fixedUpdateFacade.Tick(rawFixedDelta);
        }

        /// <inheritdoc />
        public void RunLateFrame(float rawDelta, Action<float> moduleLateUpdate)
        {
            moduleLateUpdate?.Invoke(rawDelta);
            _lateUpdateFacade.Tick(rawDelta);
        }
    }
}
