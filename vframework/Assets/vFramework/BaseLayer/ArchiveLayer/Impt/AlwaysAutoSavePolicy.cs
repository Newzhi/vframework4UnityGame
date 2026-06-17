namespace BaseLayer.Archive
{
    /// <summary>
    /// 默认自动存档策略：始终允许（热更可替换）。
    /// </summary>
    public sealed class AlwaysAutoSavePolicy : IAutoSavePolicy
    {
        public bool CanAutoSaveNow() => true;
    }
}
