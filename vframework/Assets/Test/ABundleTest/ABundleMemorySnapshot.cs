using System;
using System.Collections.Generic;
using System.Text;

namespace vFramework.Test.ABundleTest
{
    /// <summary>运行时内存与 AB 加载状态快照。</summary>
    [Serializable]
    public class ABundleMemorySnapshot
    {
        public string Tag;
        public string Timestamp;
        public long MonoUsedBytes;
        public long TotalAllocatedBytes;
        public long TotalReservedBytes;
        public int GcCollectionCount;
        public int LoadedBundleCount;
        public int CatalogLocationCount;
        public List<BundleRefEntry> BundleRefs = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{Tag}] {Timestamp}");
            sb.AppendLine($"  MonoUsed:          {FormatBytes(MonoUsedBytes)}");
            sb.AppendLine($"  TotalAllocated:    {FormatBytes(TotalAllocatedBytes)}");
            sb.AppendLine($"  TotalReserved:     {FormatBytes(TotalReservedBytes)}");
            sb.AppendLine($"  GC Collections:    {GcCollectionCount}");
            sb.AppendLine($"  LoadedBundles:     {LoadedBundleCount}");
            sb.AppendLine($"  CatalogLocations:  {CatalogLocationCount}");
            foreach (var entry in BundleRefs)
            {
                sb.AppendLine($"    {entry.BundleName}: ref={entry.RefCount}");
            }

            return sb.ToString();
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024f * 1024f):F2} MB ({bytes} B)";
            }

            if (bytes >= 1024)
            {
                return $"{bytes / 1024f:F1} KB ({bytes} B)";
            }

            return $"{bytes} B";
        }

        public long DeltaMonoUsed(ABundleMemorySnapshot baseline) => MonoUsedBytes - baseline.MonoUsedBytes;

        public long DeltaTotalAllocated(ABundleMemorySnapshot baseline) =>
            TotalAllocatedBytes - baseline.TotalAllocatedBytes;
    }

    [Serializable]
    public struct BundleRefEntry
    {
        public string BundleName;
        public int RefCount;
    }
}
