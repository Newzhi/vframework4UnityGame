namespace BaseFramework.BaseGameRoot
{
    /// <summary>
    /// <see cref="IFixedUpdateFacade"/> 默认实现。
    /// </summary>
    public sealed class FixedUpdateFacade : IFixedUpdateFacade
    {
        private readonly UpdatablePhaseFacade<IFixedUpdatable> _inner =
            new UpdatablePhaseFacade<IFixedUpdatable>(static (u, d) => u.FixedUpdate(d));

        /// <inheritdoc />
        public void Add(IFixedUpdatable updatable) => _inner.Add(updatable);

        /// <inheritdoc />
        public void Remove(IFixedUpdatable updatable) => _inner.Remove(updatable);

        /// <inheritdoc />
        public void Clear() => _inner.Clear();

        /// <summary>由 <see cref="GameUpdatePipeline.RunFixedFrame"/> 调用。</summary>
        internal void Tick(float fixedDeltaTime) => _inner.Tick(fixedDeltaTime);
    }
}
