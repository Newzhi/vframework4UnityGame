namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 轻量 FixedUpdate 订阅门面。
    /// </summary>
    public interface IFixedUpdateFacade
    {
        /// <summary>订阅 FixedUpdate。</summary>
        void Add(IFixedUpdatable updatable);

        /// <summary>取消订阅。</summary>
        void Remove(IFixedUpdatable updatable);

        /// <summary>清空全部订阅。</summary>
        void Clear();
    }
}
