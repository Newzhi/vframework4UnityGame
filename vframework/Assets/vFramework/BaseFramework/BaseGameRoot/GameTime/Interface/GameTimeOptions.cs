namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="GameTimeModule"/> 构造参数；由 Bootstrap 注入初始配置。
    /// </summary>
    public sealed class GameTimeOptions
    {
        /// <summary>日历配置；为 null 时仅启用连续时刻 A，不推进 Day/Hour/Minute。</summary>
        public GameCalendarSettings CalendarSettings { get; set; }

        /// <summary>启动时 <see cref="IGameTimeClock.TimeScale"/> 初值。默认 1。</summary>
        public float InitialTimeScale { get; set; } = 1f;
    }
}
