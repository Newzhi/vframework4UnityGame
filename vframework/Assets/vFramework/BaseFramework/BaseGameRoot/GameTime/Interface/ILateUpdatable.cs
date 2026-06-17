namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 轻量 LateUpdate 订阅，由 <see cref="ILateUpdateFacade"/> 驱动。
    /// </summary>
    public interface ILateUpdatable
    {
        /// <summary>LateUpdate 相位回调；delta 为 Unity deltaTime。</summary>
        void LateUpdate(float deltaTime);
    }
}
