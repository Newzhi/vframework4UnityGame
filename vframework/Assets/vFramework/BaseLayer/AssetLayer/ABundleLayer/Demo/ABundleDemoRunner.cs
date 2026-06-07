// ABundleDemoRunner.cs — 运行时演示（Demo）
// 用途：TestAB 场景中单资源 Load/Unload 演示，默认 location「icon/3」。

using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Demo
{
    /// <summary>
    /// ABundle 加载演示：绑定 UI 按钮到 OnClickLoad / OnClickUnload，或在 Play 模式下使用屏幕按钮。
    /// 默认加载 location「icon/3」（需先打包或使用 EditorSimulation + Catalog）。
    /// </summary>
    public class ABundleDemoRunner : MonoBehaviour
    {
        #region Inspector

        [Header("规则")]
        [Tooltip("留空则使用默认规则 XML")]
        [SerializeField] string rulesXmlPath;

        [Header("加载目标")]
        [SerializeField] string location = "icon/3";
        [SerializeField] Transform spawnRoot;

        [Header("可选预览")]
        [SerializeField] RawImage iconPreview;

        [Header("Play 模式快捷按钮（无 Canvas 时可用）")]
        [SerializeField] bool showOnGuiButtons = true;

        [Header("释放方式")]
        [Tooltip("启用后通过 ABundleScopeLoader + Handle 加载，OnDestroy 自动释放")]
        [SerializeField] bool useScopeLoader = true;

        #endregion

        #region 运行时状态

        ABundleScopeLoader _scope;
        vFramework.BaseLayer.AssetLayer.IAssetHandle _handle;
        ABundleBuildRules _rules;
        Texture _loadedTexture;
        bool _isLoaded;

        #endregion

        #region Unity 生命周期

        void OnDestroy() => OnClickUnload();

        void OnGUI()
        {
            if (!showOnGuiButtons)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 220, 120), GUI.skin.box);
            GUILayout.Label("ABundle Demo");
            GUI.enabled = !_isLoaded;
            if (GUILayout.Button("加载资源", GUILayout.Height(36)))
            {
                OnClickLoad();
            }

            GUI.enabled = _isLoaded;
            if (GUILayout.Button("卸载资源", GUILayout.Height(36)))
            {
                OnClickUnload();
            }

            GUI.enabled = true;
            GUILayout.Label(_isLoaded ? $"已加载: {location}" : "未加载");
            GUILayout.EndArea();
        }

        #endregion

        #region 按钮回调

        /// <summary>绑定到「加载」Button.onClick</summary>
        public void OnClickLoad()
        {
            if (_isLoaded)
            {
                Debug.LogWarning("[ABundleDemo] 请先卸载再加载");
                return;
            }

            if (!EnsureRules())
            {
                return;
            }

            if (useScopeLoader)
            {
                _scope ??= gameObject.GetComponent<ABundleScopeLoader>() ??
                           gameObject.AddComponent<ABundleScopeLoader>();
                if (!_scope.EnsureInitialized())
                {
                    Debug.LogError("[ABundleDemo] ScopeLoader 初始化失败");
                    return;
                }

                if (!_scope.Loader.ContainsLocation(location))
                {
                    Debug.LogError(
                        $"[ABundleDemo] Catalog 无 location「{location}」。\n" +
                        "请先 ABundleBuilder 打包，或将 LoadMode 设为 EditorSimulation。");
                    return;
                }

                _handle = _scope.LoadHandle<Texture>(location);
                _loadedTexture = _handle.GetAsset<Texture>();
            }
            else
            {
                var loader = new ABundleLoader();
                loader.InitializeFromRules(_rules);
                if (!loader.IsInitialized || !loader.ContainsLocation(location))
                {
                    Debug.LogError($"[ABundleDemo] 加载失败: {location}");
                    loader.Shutdown();
                    return;
                }

                _loadedTexture = loader.LoadAsset<Texture>(location);
                loader.ReleaseAsset(location);
                loader.Shutdown();
            }

            if (_loadedTexture == null)
            {
                Debug.LogError($"[ABundleDemo] 加载失败: {location}");
                return;
            }

            if (iconPreview != null)
            {
                iconPreview.texture = _loadedTexture;
            }

            _isLoaded = true;
            var mode = useScopeLoader ? _scope.Loader.LoadMode.ToString() : _rules.LoadMode.ToString();
            Debug.Log($"[ABundleDemo] 加载成功 location={location} mode={mode} scope={useScopeLoader}");
        }

        /// <summary>绑定到「卸载」Button.onClick</summary>
        public void OnClickUnload()
        {
            if (iconPreview != null)
            {
                iconPreview.texture = null;
            }

            _loadedTexture = null;

            if (useScopeLoader && _scope != null)
            {
                if (_handle != null)
                {
                    _scope.Release(_handle);
                    _handle = null;
                }

                _scope.RecycleAll();
            }

            _isLoaded = false;
            Debug.Log("[ABundleDemo] 已卸载");
        }

        #endregion

        #region 内部

        bool EnsureRules()
        {
            if (_rules != null)
            {
                return true;
            }

            var path = string.IsNullOrWhiteSpace(rulesXmlPath)
                ? ABundleRulesXmlIO.DefaultRulesRelativePath
                : rulesXmlPath;

            if (File.Exists(ABundleRulesXmlIO.ToFullPath(path)))
            {
                _rules = ABundleRulesXmlIO.Load(path);
            }
            else
            {
                _rules = ABundleRulesXmlIO.CreateDefault();
                Debug.LogWarning($"[ABundleDemo] 未找到规则 XML，使用内存默认配置: {path}");
            }

            return _rules != null;
        }

        #endregion
    }
}
