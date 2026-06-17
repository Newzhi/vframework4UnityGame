namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 日历玩法配置：1 游戏日对应多少游戏秒、每日小时数等。
    /// 可在 Bootstrap 注入，也可运行时经 <see cref="IGameCalendar.Configure"/> 覆盖。
    /// </summary>
    public sealed class GameCalendarSettings
    {
        /// <summary>1 游戏日对应的累计游戏时间（秒）。默认 120。</summary>
        public float SecondsPerDay { get; set; } = 120f;

        /// <summary>每日小时数。默认 24。</summary>
        public int HoursPerDay { get; set; } = 24;

        /// <summary>每小时分钟数。默认 60。</summary>
        public int MinutesPerHour { get; set; } = 60;
    }
}
