using BaseFramework.BaseGameRoot;

namespace BaseLayer.Archive
{
    /// <summary>
    /// 定时自动存档：依赖 <see cref="ITimerService"/> 与 <see cref="IAutoSavePolicy"/>。
    /// </summary>
    public sealed class AutoSaveController
    {
        private readonly IArchiveService _archive;
        private readonly ITimerService _timers;
        private readonly IAutoSavePolicy _policy;
        private TimerHandle _timerHandle;

        public AutoSaveController(
            IArchiveService archive,
            ITimerService timers,
            IAutoSavePolicy policy = null)
        {
            _archive = archive;
            _timers = timers;
            _policy = policy ?? new AlwaysAutoSavePolicy();
        }

        /// <summary>启动周期自动存档。</summary>
        public void Start(float intervalSeconds)
        {
            Stop();
            if (intervalSeconds <= 0f)
                return;

            _timerHandle = _timers.Repeat(intervalSeconds, OnAutoSaveTick);
        }

        /// <summary>停止自动存档 Timer。</summary>
        public void Stop()
        {
            if (_timerHandle.IsValid)
            {
                _timers.Cancel(_timerHandle);
                _timerHandle = TimerHandle.Invalid;
            }
        }

        private void OnAutoSaveTick()
        {
            if (!_policy.CanAutoSaveNow())
                return;

            _archive.SaveAuto("自动存档");
        }
    }
}
