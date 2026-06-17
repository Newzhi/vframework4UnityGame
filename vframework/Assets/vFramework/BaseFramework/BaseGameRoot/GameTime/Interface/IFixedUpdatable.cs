namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 轻量 FixedUpdate 订阅，由 <see cref="IFixedUpdateFacade"/> 驱动。
    /// </summary>
    public interface IFixedUpdatable
    {
        /// <summary>FixedUpdate 相位回调；delta 为 Unity fixedDeltaTime。</summary>
        void FixedUpdate(float fixedDeltaTime);
    }
}
