namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IGameMomentProvider"/> 默认实现：只读聚合 Clock / Timeline / Calendar。
    /// </summary>
    public sealed class GameMomentProvider : IGameMomentProvider
    {
        private readonly IGameTimeClock _clock;
        private readonly ISessionTimeline _timeline;
        private readonly IGameCalendar _calendar;

        public GameMomentProvider(
            IGameTimeClock clock,
            ISessionTimeline timeline,
            IGameCalendar calendar)
        {
            _clock = clock;
            _timeline = timeline;
            _calendar = calendar;
        }

        /// <inheritdoc />
        public GameMoment Now => new GameMoment(
            _clock.GameTime,
            _clock.Frame,
            _timeline.ChapterId,
            _calendar.IsEnabled ? _calendar.Day : 0,
            _calendar.IsEnabled ? _calendar.Hour : 0,
            _calendar.IsEnabled ? _calendar.Minute : 0);
    }
}
