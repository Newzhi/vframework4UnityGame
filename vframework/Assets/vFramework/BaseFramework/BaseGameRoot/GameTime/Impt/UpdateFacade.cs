namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IUpdateFacade"/> 默认实现。
    /// </summary>
    public sealed class UpdateFacade : IUpdateFacade
    {
        private readonly UpdatablePhaseFacade<IUpdatable> _inner =
            new UpdatablePhaseFacade<IUpdatable>(static (u, d) => u.Update(d));

        /// <inheritdoc />
        public void Add(IUpdatable updatable) => _inner.Add(updatable);

        /// <inheritdoc />
        public void Remove(IUpdatable updatable) => _inner.Remove(updatable);

        /// <inheritdoc />
        public void Clear() => _inner.Clear();

        /// <summary>由 <see cref="GameUpdatePipeline.RunFrame"/> 调用。</summary>
        internal void Tick(float deltaTime) => _inner.Tick(deltaTime);
    }
}
