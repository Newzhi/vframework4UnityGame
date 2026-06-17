namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 轻量 LateUpdate 订阅门面。
    /// </summary>
    public interface ILateUpdateFacade
    {
        /// <summary>订阅 LateUpdate。</summary>
        void Add(ILateUpdatable updatable);

        /// <summary>取消订阅。</summary>
        void Remove(ILateUpdatable updatable);

        /// <summary>清空全部订阅。</summary>
        void Clear();
    }
}
