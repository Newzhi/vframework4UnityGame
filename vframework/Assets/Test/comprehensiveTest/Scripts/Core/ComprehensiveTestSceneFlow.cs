using BaseFramework.BaseEventSys;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 综合测试场景切换：离开战斗场景时 UnloadAll（内部 DeleteAllPools）并销毁 PoolRuntime。
/// </summary>
public static class ComprehensiveTestSceneFlow
{
    public static void LoadGameScene()
    {
        CleanupBeforeSceneChange();
        SceneManager.LoadScene(ComprehensiveTestPaths.GameSceneName);
    }

    public static void ReturnToStartScene()
    {
        CleanupBeforeSceneChange();
        SceneManager.LoadScene(ComprehensiveTestPaths.StartSceneName);
    }

    public static void CleanupBeforeSceneChange()
    {
        // UnloadAll：DeleteAllPools（强制删池 + Release 句柄）→ 清 Resource / Bundle
        BundleResLoader.Instance.UnloadAll();
        DestroyPoolRuntimeIfExists();
        GameEventBus.ClearAll();
    }

    /// <summary>销毁 PoolSceneRoots 运行时根；与 PrefabPool 闲置/活跃父节点生命周期一致。</summary>
    static void DestroyPoolRuntimeIfExists()
    {
        GameObject poolRuntime = GameObject.Find(PoolSceneRootsUtil.RuntimeRootName);
        if (poolRuntime != null)
            Object.Destroy(poolRuntime);

        PoolSceneRootsUtil.ClearCache();
    }
}
