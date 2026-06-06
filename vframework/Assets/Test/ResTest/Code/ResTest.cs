using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 最小 AB 加载测试（Unity 原生 API）。
/// 流程：Mark 资源 → vFramework → Build Test AB → Play 本场景。
/// </summary>
public class ResTest : MonoBehaviour
{
    [Header("UI（load 为空时使用 legacy button）")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button unloadButton;

    [Tooltip("兼容旧场景：仅绑了一个 Button 时填这里")]
    [SerializeField] private Button button;

    [Header("实例化")]
    [SerializeField] private Transform spawnRoot;

    private readonly AbNativeBundle _bundle = new AbNativeBundle();
    private GameObject _instance;

    void Start()
    {
        var load = loadButton != null ? loadButton : button;
        if (load != null)
        {
            load.onClick.AddListener(OnLoadClick);
        }
        else
        {
            Debug.LogWarning("[ResTest] 未绑定 Load 按钮");
        }

        if (unloadButton != null)
        {
            unloadButton.onClick.AddListener(OnUnloadClick);
        }
    }

    void OnDestroy()
    {
        OnUnloadClick();
    }

    /// <summary>加载 demo/ui/testui 包并实例化 Assets/AssetBundle/UI/TestUI.prefab。</summary>
    public void OnLoadClick()
    {
        if (_instance != null)
        {
            Debug.Log("[ResTest] 实例已存在，跳过加载");
            return;
        }

        if (!_bundle.Load(AbTestConfig.UiTestUiRootBundle))
        {
            return;
        }

        var prefab = _bundle.LoadAsset<GameObject>(AbTestConfig.TestUiAssetName);
        if (prefab == null)
        {
            return;
        }

        _instance = spawnRoot != null
            ? Instantiate(prefab, spawnRoot)
            : Instantiate(prefab);

        Debug.Log("[ResTest] TestUI 实例化成功");
    }

    /// <summary>销毁实例并卸载 AB。</summary>
    public void OnUnloadClick()
    {
        if (_instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }

        // false：仅卸 AB 文件句柄；实例已 Destroy 即可
        _bundle.Unload(false);
    }
}
