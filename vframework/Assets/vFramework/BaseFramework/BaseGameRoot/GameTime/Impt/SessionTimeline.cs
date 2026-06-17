namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="ISessionTimeline"/> 默认实现；仅维护 ChapterId。
    /// </summary>
    public sealed class SessionTimeline : ISessionTimeline
    {
        public int ChapterId { get; private set; }

        /// <inheritdoc />
        public void SetChapter(int chapterId) => ChapterId = chapterId;
    }
}
