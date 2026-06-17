namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 轻量 Update 订阅门面；Tick 期间 Add / Remove 延迟到本帧结束后生效。
    /// </summary>
    public interface IUpdateFacade
    {
        /// <summary>订阅 Update；同一实例不重复添加。</summary>
        void Add(IUpdatable updatable);

        /// <summary>取消订阅。</summary>
        void Remove(IUpdatable updatable);

        /// <summary>清空全部订阅。</summary>
        void Clear();
    }
}
