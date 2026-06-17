using System;

namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 定时器句柄；用于 <see cref="ITimerService.Cancel"/>。
    /// </summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        /// <summary>无效句柄（Id 为 0）。</summary>
        public static readonly TimerHandle Invalid = new TimerHandle(0);

        /// <summary>内部唯一 ID；0 表示无效。</summary>
        public int Id { get; }

        /// <summary>是否为有效句柄。</summary>
        public bool IsValid => Id != 0;

        /// <summary>由 <see cref="ITimerService"/> 内部分配。</summary>
        public TimerHandle(int id) => Id = id;

        public bool Equals(TimerHandle other) => Id == other.Id;

        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);

        public override int GetHashCode() => Id;

        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);

        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
    }
}
