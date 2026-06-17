namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IGameTimeClock"/> 默认实现。
    /// </summary>
    public sealed class GameTimeClock : IGameTimeClock
    {
        public float RealTime { get; private set; }
        public float GameTime { get; private set; }
        public float DeltaTime { get; private set; }
        public long Frame { get; private set; }
        public float TimeScale { get; set; } = 1f;
        public bool IsPaused { get; set; }

        /// <inheritdoc />
        public void Advance(float rawDelta)
        {
            if (rawDelta < 0f)
                rawDelta = 0f;

            RealTime += rawDelta;

            if (IsPaused || TimeScale <= 0f)
            {
                DeltaTime = 0f;
                return;
            }

            DeltaTime = rawDelta * TimeScale;
            GameTime += DeltaTime;
            Frame++;
        }

        /// <inheritdoc />
        public void Reset()
        {
            RealTime = 0f;
            GameTime = 0f;
            DeltaTime = 0f;
            Frame = 0;
        }
    }
}
