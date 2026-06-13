using BaseFramework.BaseEventSys;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 综合测试场景切换：离开战斗场景时 UnloadAll 并销毁 PoolRuntime。
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
        BundleResLoader.Instance.UnloadAll();
        DestroyPoolRuntimeIfExists();
        GameEventBus.ClearAll();
    }

    static void DestroyPoolRuntimeIfExists()
    {
        GameObject poolRuntime = GameObject.Find(PoolSceneRoots.RuntimeRootName);
        if (poolRuntime != null)
            Object.Destroy(poolRuntime);

        PoolSceneRoots.ClearCache();
    }
}
