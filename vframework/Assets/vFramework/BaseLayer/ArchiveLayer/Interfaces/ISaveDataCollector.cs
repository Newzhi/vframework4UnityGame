namespace BaseLayer.Archive
{
    /// <summary>
    /// 热更层实现：从当前游戏状态收集 opaque payload（序列化由业务决定）。
    /// </summary>
    public interface ISaveDataCollector
    {
        /// <summary>收集存档二进制；返回 null 或空数组表示取消保存。</summary>
        byte[] Collect();
    }
}
