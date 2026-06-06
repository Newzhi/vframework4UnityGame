using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 最小 AB 加载测试：先菜单 Build Test AB，再 Play 本场景点按钮。
/// </summary>
public class ResTest : MonoBehaviour
{
    public Button button;

    // 与 HighTail.prefab 上的 AssetBundle 名一致：test/hightail
    const string BundleRelativePath = "AssetBundles/test/hightail";

    void Start()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        string path = Path.Combine(Application.streamingAssetsPath, BundleRelativePath);
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Debug.LogError($"[AB] 加载失败，请先执行 vFramework → Build Test AB。路径: {path}");
            return;
        }

        var prefab = bundle.LoadAsset<GameObject>("HighTail");
        if (prefab == null)
        {
            Debug.LogError("[AB] 包内找不到 HighTail，请查看 test/hightail.manifest");
            return;
        }

        Instantiate(prefab);
        Debug.Log("[AB] HighTail 加载成功");
    }
}
