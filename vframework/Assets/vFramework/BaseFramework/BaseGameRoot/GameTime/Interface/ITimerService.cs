using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 基于游戏时间的 Delay / Repeat 定时器。
    /// 由 Pipeline 在 Clock 推进后 Tick，暂停或 TimeScale=0 时不触发。
    /// </summary>
    public interface ITimerService
    {
        /// <summary>游戏时间 <paramref name="seconds"/> 后执行一次 <paramref name="callback"/>。</summary>
        TimerHandle Delay(float seconds, Action callback);

        /// <summary>每隔 <paramref name="intervalSeconds"/> 游戏时间重复执行。</summary>
        TimerHandle Repeat(float intervalSeconds, Action callback);

        /// <summary>取消指定定时器；无效句柄无操作。</summary>
        void Cancel(TimerHandle handle);

        /// <summary>取消全部定时器。</summary>
        void CancelAll();
    }
}
