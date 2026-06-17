using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 日历时刻 (B)：Day / Hour / Minute，按游戏时间 delta 推进。
    /// 须先 <see cref="Configure"/> 后 <see cref="IsEnabled"/> 为 true。
    /// </summary>
    public interface IGameCalendar
    {
        /// <summary>是否已通过 Configure 启用日历推进。</summary>
        bool IsEnabled { get; }

        /// <summary>当前游戏日（从 0 或启动日计数，取决于业务解读）。</summary>
        int Day { get; }

        /// <summary>当前游戏小时 [0, HoursPerDay)。</summary>
        int Hour { get; }

        /// <summary>当前游戏分钟 [0, MinutesPerHour)。</summary>
        int Minute { get; }

        /// <summary>跨日时触发，参数为新 Day 值。</summary>
        event Action<int> OnDayChanged;

        /// <summary>启用或覆盖日历配置（Bootstrap 注入或运行时跳日调试）。</summary>
        void Configure(GameCalendarSettings settings);

        /// <summary>按游戏 delta 推进日历；由 Pipeline 在 Update 相位调用。</summary>
        void Advance(float gameDelta);
    }
}
