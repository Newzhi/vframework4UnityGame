using UnityEngine;

/// <summary>
/// 刷怪：仅生成敌人实例，不引用 <see cref="enemyTest"/>。
/// 敌人逻辑、子弹池、死亡回收由预制体上的 <see cref="enemyTest"/> 自管。
/// </summary>
public class enemyManager : MonoBehaviour
{
    #region 游戏逻辑

    const string EnemyPath = "Model/Prefabs/enemy";
    const int EnemyMaxInactive = 12;
    const float SpawnInterval = 2.5f;
    const float SpawnRadius = 18f;
    const int MaxActiveEnemies = 8;

    PrefabPool enemyPool;
    IAssetHandle enemyPrefabHandle;
    Transform player;
    float nextSpawnTime;

    void Start()
    {
        InitTestSpawnMode();
        LogTestSpawnMode();

        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("enemyManager: EnsureReady failed.");
            return;
        }

        if (IsPooledSpawnMode())
            enemyPool = BundleResLoader.Instance.GetOrCreatPool(EnemyPath, maxInactiveCapacity: EnemyMaxInactive);
        else
            enemyPrefabHandle = BundleResLoader.Instance.Load<GameObject>(EnemyPath);

        player = GameObject.Find("Player")?.transform;
        if (player == null)
            Debug.LogError("enemyManager: Player not found.");

        nextSpawnTime = Time.time + 1f;
    }

    void OnDestroy()
    {
        if (IsPooledSpawnMode())
            BundleResLoader.Instance.DestroyPoolByLoadPath(EnemyPath);
        else
        {
            enemyPrefabHandle?.Release();
            enemyPrefabHandle = null;
        }
    }

    void Update()
    {
        if (player == null || Time.time < nextSpawnTime)
            return;

        if (GetActiveEnemyCount() >= MaxActiveEnemies)
            return;

        if (IsPooledSpawnMode() && enemyPool == null)
            return;

        if (!IsPooledSpawnMode() && enemyPrefabHandle == null)
            return;

        SpawnEnemy();
        nextSpawnTime = Time.time + SpawnInterval;
    }

    int GetActiveEnemyCount()
    {
        if (IsPooledSpawnMode())
            return enemyPool != null ? enemyPool.ActiveCount : 0;

        return enemyTest.DirectAliveCount;
    }

    void SpawnEnemy()
    {
        Vector3 offset = Random.insideUnitSphere * SpawnRadius;
        offset.y = 0f;
        Vector3 spawnPos = player.position + offset;

        if (IsPooledSpawnMode())
            enemyPool?.GetObj(spawnPos, Quaternion.identity);
        else
            enemyPrefabHandle?.InstantiateAt(spawnPos, Quaternion.identity, null);
    }

    #endregion

    #region 综合测试

    ComprehensiveTestDebugConfig.EnemySpawnMode spawnMode;

    void InitTestSpawnMode()
    {
        spawnMode = ComprehensiveTestDebugConfig.ResolveEnemySpawnMode();
    }

    void LogTestSpawnMode()
    {
        ComprehensiveTestLogger.LogEnemySpawnMode(spawnMode);
    }

    bool IsPooledSpawnMode()
    {
        return spawnMode == ComprehensiveTestDebugConfig.EnemySpawnMode.Pooled;
    }

    #endregion
}
