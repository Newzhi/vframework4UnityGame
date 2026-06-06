using System;
using System.Collections.Generic;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    #region Catalog

    [Serializable]
    public class AssetCatalog
    {
        public int Version = 1;
        public string BuildTime;
        public string Platform;

        public List<BundleInfo> Bundles = new();
        public List<AssetLocationEntry> Locations = new();

        [NonSerialized] Dictionary<string, AssetLocationEntry> _locationMap;
        [NonSerialized] Dictionary<string, BundleInfo> _bundleMap;

        public void BuildRuntimeIndex()
        {
            _locationMap = new Dictionary<string, AssetLocationEntry>(Locations.Count);
            foreach (var entry in Locations)
            {
                if (string.IsNullOrEmpty(entry?.Location))
                {
                    continue;
                }

                _locationMap[entry.Location] = entry;
            }

            _bundleMap = new Dictionary<string, BundleInfo>(Bundles.Count);
            foreach (var bundle in Bundles)
            {
                if (string.IsNullOrEmpty(bundle?.BundleName))
                {
                    continue;
                }

                _bundleMap[bundle.BundleName] = bundle;
            }
        }

        public bool TryGetLocation(string location, out AssetLocationEntry entry)
        {
            EnsureIndex();
            return _locationMap.TryGetValue(location, out entry);
        }

        public bool TryGetBundle(string bundleName, out BundleInfo info)
        {
            EnsureIndex();
            return _bundleMap.TryGetValue(bundleName, out info);
        }

        public string[] GetDependencies(string bundleName)
        {
            if (TryGetBundle(bundleName, out var info) && info.Dependencies != null)
            {
                return info.Dependencies;
            }

            return Array.Empty<string>();
        }

        void EnsureIndex()
        {
            if (_locationMap == null || _bundleMap == null)
            {
                BuildRuntimeIndex();
            }
        }
    }

    [Serializable]
    public class BundleInfo
    {
        public string BundleName;
        public string Hash;
        public long Size;
        public string[] Dependencies = Array.Empty<string>();
        public string FileName;
    }

    [Serializable]
    public class AssetLocationEntry
    {
        public string Location;
        public string BundleName;
        public string AssetName;
        public string AssetType;
        public string SourceAssetPath;
    }

    #endregion

    #region 打包报告

    [Serializable]
    public class ABundleBuildReport
    {
        public bool Success;
        public string Platform;
        public string LoadMode;
        public string OutputPath;
        public string CatalogPath;
        public string ReportPath;
        public string BuildTime;
        public double DurationSeconds;
        public int BundleCount;
        public int LocationCount;
        public long TotalSizeBytes;
        public List<ABundleReportEntry> Bundles = new();
        public List<string> Warnings = new();
        public List<string> Errors = new();
    }

    [Serializable]
    public class ABundleReportEntry
    {
        public string BundleName;
        public long SizeBytes;
        public string Hash;
        public string[] Dependencies = Array.Empty<string>();
    }

    #endregion
}
