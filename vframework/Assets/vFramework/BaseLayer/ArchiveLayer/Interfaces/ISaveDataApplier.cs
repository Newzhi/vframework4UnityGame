namespace BaseLayer.Archive
{
    /// <summary>
    /// 热更层实现：将 payload 灌回 Model / Proxy 等。
    /// </summary>
    public interface ISaveDataApplier
    {
        void Apply(byte[] payload);
    }
}
