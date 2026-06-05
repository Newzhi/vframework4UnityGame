using UnityEngine;

namespace vFramework.BaseLayer.AssetLayer
{
    internal sealed class RefCountedAssetHandle : IAssetHandle
    {
        private AssetCacheEntry _entry;
        private ResMgr _owner;
        private bool _released;

        public RefCountedAssetHandle(AssetCacheEntry entry, ResMgr owner)
        {
            _entry = entry;
            _owner = owner;
        }

        public string Location => _entry?.Location;

        public bool IsValid => !_released && _entry != null && _entry.Asset != null;

        public Object RawObject => _entry?.Asset;

        public T GetAsset<T>() where T : Object
        {
            if (!IsValid)
            {
                return null;
            }

            return _entry.Asset as T;
        }

        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _owner?.ReleaseEntry(_entry);
            _entry = null;
            _owner = null;
        }
    }
}
