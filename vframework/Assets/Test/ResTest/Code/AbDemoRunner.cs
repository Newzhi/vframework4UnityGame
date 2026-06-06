using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AB 三个问题的运行时演示。详见 Assets/Test/ResTest/AB学习演示说明.md
/// </summary>
public class AbDemoRunner : MonoBehaviour
{
    [Header("可选 UI 绑定（见说明文档）")]
    [SerializeField] private RawImage iconPreview;
    [SerializeField] private Transform spawnRoot;

    private GameObject _spawnedInstance;

    // ═══════════════════════════════════════════════════════════════
    // 问题1：AB 有没有「大包套小包」？
    // ═══════════════════════════════════════════════════════════════
    // 答：没有「AB 文件里再嵌 AB 文件」。一个 .ab 文件是一个 AssetBundle，
    //     里面可以装【多份 Unity 资源】(贴图、prefab…)，像一个大箱子里 many 物品。
    //     加载方式：LoadFromFile(箱名) 一次 → LoadAsset(物品名) 多次。
    // ═══════════════════════════════════════════════════════════════

    /// <summary>绑定到 Button「Q1-从 icon 包加载 3.png」</summary>
    public void Demo1_LoadOneIconFromMultiAssetBundle()
    {
        // Step1: AssetBundle.LoadFromFile — 加载「整个 icon 包」这一份文件
        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.IconBundle);
        if (bundle == null)
        {
            return;
        }

        // Step2: AssetBundle.LoadAsset&lt;T&gt; — 从已加载的包内按【资源名】取其中一个
        // 包 demo/icon 里还有 1.png、2.png… 但这里只取 "3"
        const string assetNameInBundle = "3";
        var tex = bundle.LoadAsset<Texture>(assetNameInBundle);
        if (tex == null)
        {
            Debug.LogError($"[Q1] 包 {AbTestConfig.IconBundle} 内无 Texture: {assetNameInBundle}，请查 manifest");
            return;
        }

        if (iconPreview != null)
        {
            iconPreview.texture = tex;
        }

        Debug.Log(
            $"[Q1] 已从「一个 AB 文件 {AbTestConfig.IconBundle}」中取出其中一份资源 {assetNameInBundle}。" +
            " 这不是套包，是单包多资源。");
    }

    // ═══════════════════════════════════════════════════════════════
    // 问题2：同名资源怎么处理？
    // ═══════════════════════════════════════════════════════════════

    /// <summary>绑定到 Button「Q2-加载 UI/TestUI.prefab」</summary>
    public void Demo2_LoadRootTestUI()
    {
        LoadTestUiPrefabFromBundle(
            AbTestConfig.UiTestUiRootBundle,
            "Assets/AssetBundle/UI/TestUI.prefab");
    }

    /// <summary>绑定到 Button「Q2-加载 UI/Test/TestUI.prefab」</summary>
    public void Demo2_LoadAltTestUI()
    {
        LoadTestUiPrefabFromBundle(
            AbTestConfig.UiTestUiAltBundle,
            "Assets/AssetBundle/UI/Test/TestUI.prefab");
    }

    private void LoadTestUiPrefabFromBundle(string bundleName, string sourceAssetPath)
    {
        ClearSpawned();

        var bundle = AbManifestLoader.LoadBundleWithDependencies(bundleName);
        if (bundle == null)
        {
            return;
        }

        // 两个 prefab 文件名都是 TestUI，但放在【不同 AB 包】里 → 用包名区分，不会混
        var prefab = bundle.LoadAsset<GameObject>(AbTestConfig.TestUiAssetName);
        if (prefab == null)
        {
            Debug.LogError($"[Q2] {bundleName} 内无 {AbTestConfig.TestUiAssetName}");
            return;
        }

        _spawnedInstance = spawnRoot != null
            ? Instantiate(prefab, spawnRoot)
            : Instantiate(prefab);

        Debug.Log($"[Q2] 已从包 [{bundleName}] 加载同名 Prefab [{AbTestConfig.TestUiAssetName}]，源路径 {sourceAssetPath}");
    }

    /// <summary>绑定到 Button「Q2-同名不同类型 lambert2」</summary>
    public void Demo2_SameNameDifferentType()
    {
        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.JiMatBundle);
        if (bundle == null)
        {
            return;
        }

        var name = AbTestConfig.Lambert2AssetName;

        // LoadAsset 带泛型：同名时按【类型】区分
        // lambert2.mat → LoadAsset&lt;Material&gt; 有值；LoadAsset&lt;GameObject&gt; 为 null
        var mat = bundle.LoadAsset<Material>(name);
        var go = bundle.LoadAsset<GameObject>(name);

        var sb = new StringBuilder();
        sb.AppendLine($"[Q2] 包 {AbTestConfig.JiMatBundle} 内 Name={name}");
        sb.AppendLine($"  LoadAsset<Material>: {(mat != null ? mat.name : "null")}");
        sb.AppendLine($"  LoadAsset<GameObject>: {(go != null ? go.name : "null")}");
        Debug.Log(sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════════
    // 问题3：A 包引用 B 包的资源怎么办？
    // ═══════════════════════════════════════════════════════════════
    // 答：打包时 Unity 记录 AB 间依赖。运行时须【先 Load 依赖包，再 Load 主包】，
    //     否则主包内 Prefab 的材质/网格可能缺失（粉红、Missing）。
    //     API：AssetBundleManifest.GetAllDependencies("demo/model/ji")
    // ═══════════════════════════════════════════════════════════════

    /// <summary>绑定到 Button「Q3-带依赖加载 Ji.prefab」</summary>
    public void Demo3_LoadJiWithDependencies()
    {
        ClearSpawned();
        AbManifestLoader.UnloadAll(false);

        var manifest = AbManifestLoader.GetManifest();
        if (manifest != null)
        {
            var deps = manifest.GetAllDependencies(AbTestConfig.JiPrefabBundle);
            Debug.Log($"[Q3] {AbTestConfig.JiPrefabBundle} 的 AB 依赖: [{string.Join(", ", deps)}]");
        }

        // LoadBundleWithDependencies 内部会先 Load 依赖包，再 Load 主包
        var bundle = AbManifestLoader.LoadBundleWithDependencies(AbTestConfig.JiPrefabBundle);
        if (bundle == null)
        {
            return;
        }

        Debug.Log($"[Q3] 当前已加载的包: [{string.Join(", ", AbManifestLoader.GetLoadedBundleNames())}]");

        var prefab = bundle.LoadAsset<GameObject>(AbTestConfig.JiPrefabAssetName);
        if (prefab == null)
        {
            Debug.LogError($"[Q3] 找不到 {AbTestConfig.JiPrefabAssetName}");
            return;
        }

        _spawnedInstance = spawnRoot != null
            ? Instantiate(prefab, spawnRoot)
            : Instantiate(prefab);

        Debug.Log("[Q3] Ji 已实例化。若依赖包未先加载，模型会缺材质/网格。");
    }

    /// <summary>绑定到 Button「Q3-故意跳过依赖（对比实验）」</summary>
    public void Demo3_LoadJiWithoutDependencies()
    {
        ClearSpawned();
        AbManifestLoader.UnloadAll(false);

        // 故意只 Load 主包、不 Load 依赖；须走 Loader 缓存，禁止裸 LoadFromFile
        var bundle = AbManifestLoader.LoadBundleMainOnly(AbTestConfig.JiPrefabBundle);
        if (bundle == null)
        {
            return;
        }

        var prefab = bundle.LoadAsset<GameObject>(AbTestConfig.JiPrefabAssetName);
        if (prefab != null)
        {
            _spawnedInstance = Instantiate(prefab, spawnRoot);
            Debug.LogWarning("[Q3] 已跳过依赖加载 Ji，请观察材质/网格是否异常（对比 Demo3_LoadJiWithDependencies）");
        }
    }

    // ── 加载 Prefab 后替换包内子资源（Background 贴图）──

    /// <summary>绑定到 Button「附加-换 Background 贴图」；需实例上挂 TestUI 且已 Demo2 加载</summary>
    public void DemoExtra_ReplaceBackgroundOnSpawnedTestUI()
    {
        if (_spawnedInstance == null)
        {
            Debug.LogWarning("[Extra] 请先 Demo2 加载 TestUI");
            return;
        }

        var testUi = _spawnedInstance.GetComponent<TestUI>();
        if (testUi == null)
        {
            Debug.LogWarning("[Extra] 实例上没有 TestUI 组件，请加载 UI/Test/TestUI.prefab");
            return;
        }

        testUi.OnChangeBackground();
    }

    /// <summary>绑定到 Button「卸载全部 AB」</summary>
    public void Demo_UnloadAll()
    {
        ClearSpawned();
        AbManifestLoader.UnloadAll(false);
    }

    private void ClearSpawned()
    {
        if (_spawnedInstance != null)
        {
            Destroy(_spawnedInstance);
            _spawnedInstance = null;
        }
    }

    void OnDestroy()
    {
        Demo_UnloadAll();
    }
}
