namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 连续时刻 (A)：章节 / 关卡标识，与 <see cref="IGameTimeClock"/> 共享 GameTime / Frame。
    /// </summary>
    public interface ISessionTimeline
    {
        /// <summary>当前章节 / 关卡 ID，供存档与进度同步。</summary>
        int ChapterId { get; }

        /// <summary>切换章节；不重置 GameTime / Frame。</summary>
        void SetChapter(int chapterId);
    }
}
