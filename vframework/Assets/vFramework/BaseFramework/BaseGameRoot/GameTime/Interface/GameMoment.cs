namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏时刻只读快照：连续时刻 (A) + 日历时刻 (B)。
    /// 由 <see cref="IGameMomentProvider.Now"/> 返回，适合存档与 UI 展示。
    /// </summary>
    public readonly struct GameMoment
    {
        /// <summary>累计游戏时间（秒，受 TimeScale / 暂停影响）。</summary>
        public float GameTime { get; }

        /// <summary>自启动以来已推进的游戏帧数（暂停时不递增）。</summary>
        public long Frame { get; }

        /// <summary>当前章节 / 关卡标识（连续时刻 A）。</summary>
        public int ChapterId { get; }

        /// <summary>游戏内日（日历 B；未启用日历时为 0）。</summary>
        public int Day { get; }

        /// <summary>游戏内小时（日历 B；未启用日历时为 0）。</summary>
        public int Hour { get; }

        /// <summary>游戏内分钟（日历 B；未启用日历时为 0）。</summary>
        public int Minute { get; }

        /// <summary>构造完整时刻快照。</summary>
        public GameMoment(
            float gameTime,
            long frame,
            int chapterId,
            int day,
            int hour,
            int minute)
        {
            GameTime = gameTime;
            Frame = frame;
            ChapterId = chapterId;
            Day = day;
            Hour = hour;
            Minute = minute;
        }
    }
}
