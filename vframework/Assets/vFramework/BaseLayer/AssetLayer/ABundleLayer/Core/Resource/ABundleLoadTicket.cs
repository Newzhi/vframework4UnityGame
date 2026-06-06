using System;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// 一次包加载操作持有的引用票据。Release 时按票据释放整条依赖链，避免依赖包泄漏。
    /// </summary>
    public sealed class ABundleLoadTicket
    {
        public string MainBundleName { get; internal set; }
        public string[] RetainedBundleNames { get; internal set; } = Array.Empty<string>();
        public bool IsValid { get; internal set; }

        internal void Invalidate() => IsValid = false;
    }
}
