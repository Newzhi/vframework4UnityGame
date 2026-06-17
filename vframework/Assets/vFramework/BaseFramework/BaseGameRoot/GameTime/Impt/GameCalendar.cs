using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IGameCalendar"/> 默认实现：按 <see cref="GameCalendarSettings"/> 将游戏秒换算为 Day/Hour/Minute。
    /// </summary>
    public sealed class GameCalendar : IGameCalendar
    {
        private GameCalendarSettings _settings;

        /// <summary>当前分钟内已累计的游戏秒，满一分钟则进位。</summary>
        private float _secondsIntoMinute;

        public bool IsEnabled { get; private set; }
        public int Day { get; private set; }
        public int Hour { get; private set; }
        public int Minute { get; private set; }

        /// <inheritdoc />
        public event Action<int> OnDayChanged;

        /// <inheritdoc />
        public void Configure(GameCalendarSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
            IsEnabled = true;
        }

        /// <inheritdoc />
        public void Advance(float gameDelta)
        {
            if (!IsEnabled || gameDelta <= 0f)
                return;

            float secondsPerMinute = GetSecondsPerMinute();
            if (secondsPerMinute <= 0f)
                return;

            _secondsIntoMinute += gameDelta;

            while (_secondsIntoMinute >= secondsPerMinute)
            {
                _secondsIntoMinute -= secondsPerMinute;
                AdvanceMinute();
            }
        }

        private float GetSecondsPerMinute()
        {
            int minutesPerDay = _settings.HoursPerDay * _settings.MinutesPerHour;
            if (minutesPerDay <= 0)
                return 0f;

            return _settings.SecondsPerDay / minutesPerDay;
        }

        private void AdvanceMinute()
        {
            Minute++;
            if (Minute < _settings.MinutesPerHour)
                return;

            Minute = 0;
            Hour++;
            if (Hour < _settings.HoursPerDay)
                return;

            Hour = 0;
            Day++;
            OnDayChanged?.Invoke(Day);
        }
    }
}
