namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// 轻量 Update 订阅，由 <see cref="IUpdateFacade"/> 驱动，不必实现完整 <see cref="IGameModule"/>。
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>每帧 Update 相位回调；delta 为游戏时间 delta。</summary>
        void Update(float deltaTime);
    }
}
