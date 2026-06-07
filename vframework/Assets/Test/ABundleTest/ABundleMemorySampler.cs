using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using vFramework.BaseLayer.AssetLayer.ABundleLayer;

namespace vFramework.Test.ABundleTest
{
    /// <summary>采集 Unity Profiler 内存与 ABundleLoader 包引用状态。</summary>
    public static class ABundleMemorySampler
    {
        public static ABundleMemorySnapshot Take(ABundleLoader loader, string tag)
        {
            var snapshot = new ABundleMemorySnapshot
            {
                Tag = tag,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                MonoUsedBytes = Profiler.GetMonoUsedSizeLong(),
                TotalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                TotalReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                GcCollectionCount = GC.CollectionCount(0),
                CatalogLocationCount = loader?.Catalog?.Locations?.Count ?? 0
            };

            if (loader != null && loader.IsInitialized)
            {
                var bundleNames = loader.GetLoadedBundleNames();
                snapshot.LoadedBundleCount = bundleNames.Length;
                foreach (var name in bundleNames)
                {
                    snapshot.BundleRefs.Add(new BundleRefEntry
                    {
                        BundleName = name,
                        RefCount = loader.GetBundleRefCount(name)
                    });
                }
            }

            return snapshot;
        }

        public static void ForceCleanup()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
