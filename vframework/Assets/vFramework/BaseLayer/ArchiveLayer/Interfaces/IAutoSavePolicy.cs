namespace BaseLayer.Archive
{
    /// <summary>
    /// 自动存档策略：由热更层决定是否允许本次写入。
    /// </summary>
    public interface IAutoSavePolicy
    {
        bool CanAutoSaveNow();
    }
}
