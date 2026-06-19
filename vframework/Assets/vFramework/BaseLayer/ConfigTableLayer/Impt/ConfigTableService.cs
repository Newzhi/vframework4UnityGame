namespace BaseLayer.ConfigTable
{
    /// <inheritdoc />
    public sealed class ConfigTableService : IConfigTableService
    {
        /// <inheritdoc />
        public bool IsReady { get; }

        public ConfigTableService(bool isReady)
        {
            IsReady = isReady;
        }
    }
}
