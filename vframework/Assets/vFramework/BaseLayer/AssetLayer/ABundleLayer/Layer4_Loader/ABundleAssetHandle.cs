// ABundleAssetHandle.cs — ④ 加载器（Layer4_Loader）
// 用途：单次 Load 返回的句柄，Release() 释放其持有的 LoadTicket（含依赖链）。

using System;
using UnityEngine;
using vFramework.BaseLayer.AssetLayer;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// 一次 Load 对应的引用句柄。Release 释放其持有的 LoadTicket（含依赖链）。
    /// </summary>
    public sealed class ABundleAssetHandle : IAssetHandle
    {
        #region 字段

        readonly string _location;
        readonly Action<ABundleLoadTicket> _releaseTicket;

        UnityEngine.Object _asset;
        ABundleLoadTicket _ticket;
        bool _released;

        #endregion

        #region 构造

        internal ABundleAssetHandle(
            string location,
            UnityEngine.Object asset,
            ABundleLoadTicket ticket,
            Action<ABundleLoadTicket> releaseTicket)
        {
            _location = location;
            _asset = asset;
            _ticket = ticket;
            _releaseTicket = releaseTicket;
        }

        internal static ABundleAssetHandle Invalid(string location) =>
            new(location, null, null, null);

        #endregion

        #region IAssetHandle

        public string Location => _location;

        public bool IsValid => !_released && _asset != null;

        public UnityEngine.Object RawObject => _asset;

        public T GetAsset<T>() where T : UnityEngine.Object => _asset as T;

        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;

            if (_ticket != null && _ticket.IsValid)
            {
                _releaseTicket?.Invoke(_ticket);
            }

            _ticket = null;
            _asset = null;
        }

        #endregion
    }
}
