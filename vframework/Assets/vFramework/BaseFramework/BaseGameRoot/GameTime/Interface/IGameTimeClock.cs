namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 游戏时钟：RealTime / GameTime、TimeScale、暂停与帧计数。
    /// 每帧由 <see cref="IGameUpdatePipeline.RunFrame"/> 调用 <see cref="Advance"/>。
    /// </summary>
    public interface IGameTimeClock
    {
        /// <summary>不受 TimeScale / 暂停影响的累计真实时间（秒）。</summary>
        float RealTime { get; }

        /// <summary>受 TimeScale 影响的累计游戏时间（秒）。</summary>
        float GameTime { get; }

        /// <summary>上一帧游戏 delta（rawDelta × TimeScale；暂停时为 0）。</summary>
        float DeltaTime { get; }

        /// <summary>已推进的游戏帧计数（暂停时不递增）。</summary>
        long Frame { get; }

        /// <summary>游戏时间缩放；≤ 0 时 GameTime 不推进。</summary>
        float TimeScale { get; set; }

        /// <summary>为 true 时 GameTime / Frame 不推进，RealTime 仍累计。</summary>
        bool IsPaused { get; set; }

        /// <summary>推进一帧；<paramref name="rawDelta"/> 通常为 Unity Time.deltaTime。</summary>
        void Advance(float rawDelta);

        /// <summary>重置 GameTime / Frame / DeltaTime / RealTime。</summary>
        void Reset();
    }
}
