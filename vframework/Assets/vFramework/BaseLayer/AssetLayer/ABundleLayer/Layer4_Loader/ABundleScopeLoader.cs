// ABundleScopeLoader.cs — ④ 加载器（Layer4_Loader）
// 用途：单元级 MonoBehaviour 加载器，登记 Handle，OnDestroy 时自动 RecycleAll 释放资源。

using System.Collections.Generic;
using UnityEngine;
using vFramework.BaseLayer.AssetLayer;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer
{
    /// <summary>
    /// 单元级资源加载器：登记 Handle，OnDestroy 时自动 RecycleAll（等同 ResKit ResLoader）。
    /// </summary>
    public class ABundleScopeLoader : MonoBehaviour
    {
        #region Inspector

        [Tooltip("留空则使用默认规则 XML")]
        [SerializeField] string rulesXmlPath;

        [Tooltip("留空则使用规则中的 LoadMode")]
        [SerializeField] bool forceEditorSimulation;

        #endregion

        #region 字段

        readonly List<IAssetHandle> _handles = new();
        ABundleLoader _loader;
        ABundleBuildRules _rules;

        #endregion

        #region 属性

        public ABundleLoader Loader => _loader;
        public bool IsInitialized => _loader != null && _loader.IsInitialized;
        public int HandleCount => _handles.Count;

        #endregion

        #region 生命周期

        void Awake()
        {
            EnsureInitialized();
        }

        void OnDestroy()
        {
            RecycleAll();
            _loader?.Shutdown();
            _loader = null;
        }

        #endregion

        #region 初始化

        public bool EnsureInitialized()
        {
            if (_loader != null && _loader.IsInitialized)
            {
                return true;
            }

            _loader ??= new ABundleLoader();
            _rules = LoadRules();
            if (_rules == null)
            {
                Debug.LogError("[ABundleScope] 无法加载规则");
                return false;
            }

            ABundleLoadMode? mode = forceEditorSimulation ? ABundleLoadMode.EditorSimulation : null;
            _loader.InitializeFromRules(_rules, mode);
            return _loader.IsInitialized;
        }

        ABundleBuildRules LoadRules()
        {
            if (!string.IsNullOrEmpty(rulesXmlPath))
            {
                return ABundleRulesXmlIO.Load(rulesXmlPath);
            }

            var defaultPath = ABundleRulesXmlIO.DefaultRulesRelativePath;
            if (System.IO.File.Exists(ABundleRulesXmlIO.ToFullPath(defaultPath)))
            {
                return ABundleRulesXmlIO.Load(defaultPath);
            }

            return ABundleRulesXmlIO.CreateDefault();
        }

        #endregion

        #region 加载与释放

        public IAssetHandle LoadHandle<T>(string location) where T : Object
        {
            if (!EnsureInitialized())
            {
                return ABundleAssetHandle.Invalid(location);
            }

            var handle = _loader.LoadHandle<T>(location);
            if (handle.IsValid)
            {
                _handles.Add(handle);
            }

            return handle;
        }

        public T Load<T>(string location) where T : Object
        {
            var handle = LoadHandle<T>(location);
            return handle.GetAsset<T>();
        }

        public void Release(IAssetHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            handle.Release();
            _handles.Remove(handle);
        }

        public void RecycleAll()
        {
            for (var i = _handles.Count - 1; i >= 0; i--)
            {
                _handles[i]?.Release();
            }

            _handles.Clear();
        }

        #endregion
    }
}
