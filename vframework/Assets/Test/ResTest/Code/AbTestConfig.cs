/// <summary>
/// AB 演示用常量：与 Editor 菜单「Apply Demo AB Labels」写入的标签一致。
/// 修改标签后请重新执行：vFramework → Build Test AB
/// </summary>
public static class AbTestConfig
{
    public const string StreamingBundleFolder = "AssetBundles";

    /// <summary>平台总 Manifest 文件名（无扩展名），位于 StreamingBundleFolder 下。</summary>
    public const string PlatformManifestBundleFile = "AssetBundles";

    // ── 问题1：一个 AB「文件」内有多份资源（不是套娃小包）──
    /// <summary>整个 Icon 目录打一个包，内含 1.png、2.png…</summary>
    public const string IconBundle = "demo/icon";

    // ── 问题2：同名 Prefab，不同包 ──
    /// <summary>Assets/AssetBundle/UI/TestUI.prefab</summary>
    public const string UiTestUiRootBundle = "demo/ui/testui";

    /// <summary>Assets/AssetBundle/UI/Test/TestUI.prefab</summary>
    public const string UiTestUiAltBundle = "demo/ui/testui_alt";

    public const string TestUiAssetName = "TestUI";

    /// <summary>Atlas 目录（Sprite 图，TestUI.OnChangeBear 用）。</summary>
    public const string AtlasBundleName = "demo/atlas";

    // ── 问题2：同名不同类型（包内 Name 都是 lambert2）──
    /// <summary>demo/model/ji_mat 内的材质 lambert2.mat</summary>
    public const string JiMatBundle = "demo/model/ji_mat";
    public const string Lambert2AssetName = "lambert2";

    // ── 问题3：跨包依赖 ──
    /// <summary>Assets/AssetBundle/Model/Ji.prefab（依赖 ji_mat 等）</summary>
    public const string JiPrefabBundle = "demo/model/ji";
    public const string JiPrefabAssetName = "Ji";

    // ── 加载 Prefab 后换图（Background 目录）──
    public const string BackgroundBundle = "demo/background";
}
