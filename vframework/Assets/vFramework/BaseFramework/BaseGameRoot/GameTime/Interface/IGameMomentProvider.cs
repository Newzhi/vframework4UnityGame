namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 聚合 Clock + Timeline + Calendar 的只读时刻快照。
    /// </summary>
    public interface IGameMomentProvider
    {
        /// <summary>当前时刻快照；每次访问重新组装，无额外 Tick 开销以外的状态。</summary>
        GameMoment Now { get; }
    }
}
