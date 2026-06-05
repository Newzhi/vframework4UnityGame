using System.Threading.Tasks;
using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    internal sealed class AssetCacheEntry
    {
        public AssetCacheEntry(string location)
        {
            Location = location;
        }

        public string Location { get; }
        public UnityEngine.Object Asset { get; set; }
        public ILoaderHandle BackendHandle { get; set; }
        public int RefCount { get; set; }
        public Task<AssetCacheEntry> LoadingTask { get; set; }
        public System.Exception LoadError { get; set; }
    }
}
