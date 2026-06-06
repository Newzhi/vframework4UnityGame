using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    private string[] m_Backgrounds = new string[]
    {
        "Assets/AssetBundle/Background/1.png",
        "Assets/AssetBundle/Background/2.png",
        "Assets/AssetBundle/Background/3.png",
        "Assets/AssetBundle/Background/4.png",
        "Assets/AssetBundle/Background/5.png",
        "Assets/AssetBundle/Background/6.png",
        "Assets/AssetBundle/Background/7.png",
    };

    private string[] m_Roles = new string[]
    {
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_000.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_001.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_002.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_003.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_004.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_005.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_006.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_007.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_008.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_009.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_010.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_011.png",
    };

    private string[] m_Icons = new string[]
    {
        "Assets/AssetBundle/Icon/1.png",
        "Assets/AssetBundle/Icon/2.png",
        "Assets/AssetBundle/Icon/3.png",
        "Assets/AssetBundle/Icon/4.png",
        "Assets/AssetBundle/Icon/5.png",
        "Assets/AssetBundle/Icon/6.png",
        "Assets/AssetBundle/Icon/7.png",
        "Assets/AssetBundle/Icon/8.png",
        "Assets/AssetBundle/Icon/9.png",
        "Assets/AssetBundle/Icon/10.png",
        "Assets/AssetBundle/Icon/11.png",
        "Assets/AssetBundle/Icon/12.png",
        "Assets/AssetBundle/Icon/13.png",
        "Assets/AssetBundle/Icon/14.png",
        "Assets/AssetBundle/Icon/15.png",
        "Assets/AssetBundle/Icon/16.png",
        "Assets/AssetBundle/Icon/17.png",
        "Assets/AssetBundle/Icon/18.png",
        "Assets/AssetBundle/Icon/19.png",
    };

    private string m_ModelUrl = "Assets/AssetBundle/Model/Ji.prefab";

    [SerializeField]
    private Transform m_ModelRoot;

    private GameObject m_ModelGO;

    [SerializeField]
    private RawImage m_RawImage_Background = null;

    [SerializeField]
    private Image m_Image_Bear = null;

    [SerializeField]
    private RawImage m_RawImage_Icon = null;

    private int m_BackgourndIndex = -1;
    private int m_BearIndex = -1;
    private int m_IconIndex = -1;

    void Start()
    {
        m_BackgourndIndex = -1;
        m_BearIndex = -1;
        m_IconIndex = -1;
    }

    public void OnChangeBackground()
    {
        if (m_Backgrounds.Length == 0 || m_RawImage_Background == null)
        {
            return;
        }

        m_BackgourndIndex = ++m_BackgourndIndex % m_Backgrounds.Length;
        var assetName = Path.GetFileNameWithoutExtension(m_Backgrounds[m_BackgourndIndex]);

        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.BackgroundBundle);
        var tex = bundle?.LoadAsset<Texture>(assetName);
        if (tex != null)
        {
            m_RawImage_Background.texture = tex;
        }
    }

    public void OnChangeBear()
    {
        if (m_Roles.Length == 0 || m_Image_Bear == null)
        {
            return;
        }

        m_BearIndex = ++m_BearIndex % m_Roles.Length;
        var assetName = Path.GetFileNameWithoutExtension(m_Roles[m_BearIndex]);

        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.AtlasBundleName);
        var sp = bundle?.LoadAsset<Sprite>(assetName);
        if (sp != null)
        {
            m_Image_Bear.sprite = sp;
        }
    }

    public void OnChangeIcon()
    {
        if (m_Icons.Length == 0 || m_RawImage_Icon == null)
        {
            return;
        }

        m_IconIndex = ++m_IconIndex % m_Icons.Length;
        var assetName = Path.GetFileNameWithoutExtension(m_Icons[m_IconIndex]);

        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.IconBundle);
        var tex = bundle?.LoadAsset<Texture>(assetName);
        if (tex != null)
        {
            m_RawImage_Icon.texture = tex;
        }
    }

    public void OnLoadModel()
    {
        if (m_ModelGO != null || m_ModelRoot == null)
        {
            return;
        }

        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.JiPrefabBundle);
        var prefab = bundle?.LoadAsset<GameObject>(AbTestConfig.JiPrefabAssetName);
        if (prefab == null)
        {
            return;
        }

        m_ModelGO = Instantiate(prefab, m_ModelRoot);
        m_ModelGO.transform.localPosition = Vector3.zero;
        m_ModelGO.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    public void OnUnloadModel()
    {
        if (m_ModelGO == null)
        {
            return;
        }

        Destroy(m_ModelGO);
        m_ModelGO = null;
    }
}
