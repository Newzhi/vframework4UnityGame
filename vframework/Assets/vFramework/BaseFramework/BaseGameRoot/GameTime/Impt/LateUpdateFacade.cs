namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="ILateUpdateFacade"/> 默认实现。
    /// </summary>
    public sealed class LateUpdateFacade : ILateUpdateFacade
    {
        private readonly UpdatablePhaseFacade<ILateUpdatable> _inner =
            new UpdatablePhaseFacade<ILateUpdatable>(static (u, d) => u.LateUpdate(d));

        /// <inheritdoc />
        public void Add(ILateUpdatable updatable) => _inner.Add(updatable);

        /// <inheritdoc />
        public void Remove(ILateUpdatable updatable) => _inner.Remove(updatable);

        /// <inheritdoc />
        public void Clear() => _inner.Clear();

        /// <summary>由 <see cref="GameUpdatePipeline.RunLateFrame"/> 调用。</summary>
        internal void Tick(float deltaTime) => _inner.Tick(deltaTime);
    }
}
