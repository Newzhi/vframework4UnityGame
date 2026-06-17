using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 刷怪：仅生成敌人实例，不引用 <see cref="enemyTest"/>。
/// 敌人逻辑、子弹池、死亡回收由预制体上的 <see cref="enemyTest"/> 自管。
/// </summary>
public class enemyManager : MonoBehaviour
{
    #region 游戏逻辑

    const string EnemyPath = "Model/Prefabs/enemy";
    const float SpawnInterval = 1.4f;
    const int SpawnBatchSize = 2;
    const float SpawnMinRadius = 6f;
    const float SpawnMaxRadius = 20f;

    PrefabPool enemyPool;
    IAssetHandle enemyPrefabHandle;
    Transform player;
    float nextSpawnTime;
    ComprehensiveTestDebugConfig.EntitySpawnMode spawnMode;
    readonly List<Vector3> spawnOccupied = new List<Vector3>();

    void Start()
    {
        spawnMode = ComprehensiveTestDebugConfig.ResolveEntitySpawnMode();
        ComprehensiveTestLogger.LogEntitySpawnMode(spawnMode);

        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("enemyManager: EnsureReady failed.");
            return;
        }

        if (EntitySpawnHelper.IsPooled(spawnMode))
            enemyPool = PrefabPoolManager.Instance.GetOrCreatPool(
                EnemyPath,
                maxInactiveCapacity: EntitySpawnHelper.MaxEnemyCount);
        else if (!EntitySpawnHelper.IsAutoUnload(spawnMode))
            enemyPrefabHandle = BundleResLoader.Instance.Load<GameObject>(EnemyPath);

        player = GameObject.Find("Player")?.transform;
        if (player == null)
            Debug.LogError("enemyManager: Player not found.");

        nextSpawnTime = Time.time + 0.6f;
    }

    void OnDestroy()
    {
        if (EntitySpawnHelper.IsPooled(spawnMode))
            PrefabPoolManager.Instance.ReleasePoolShare(EnemyPath);
        else if (!EntitySpawnHelper.IsAutoUnload(spawnMode))
        {
            enemyPrefabHandle?.Release();
            enemyPrefabHandle = null;
        }
    }

    void Update()
    {
        if (player == null || Time.time < nextSpawnTime)
            return;

        if (GetActiveEnemyCount() >= EntitySpawnHelper.MaxEnemyCount)
            return;

        if (EntitySpawnHelper.IsPooled(spawnMode) && enemyPool == null)
            return;

        if (!EntitySpawnHelper.IsPooled(spawnMode) && !EntitySpawnHelper.IsAutoUnload(spawnMode)
            && enemyPrefabHandle == null)
            return;

        SpawnEnemyBatch();
        nextSpawnTime = Time.time + SpawnInterval;
    }

    int GetActiveEnemyCount()
    {
        if (EntitySpawnHelper.IsPooled(spawnMode))
            return enemyPool != null ? enemyPool.ActiveCount : 0;

        return enemyTest.DirectAliveCount;
    }

    void SpawnEnemyBatch()
    {
        spawnOccupied.Clear();
        EntitySpawnHelper.CollectOccupiedPositions(spawnOccupied);

        for (int i = 0; i < SpawnBatchSize; i++)
        {
            if (GetActiveEnemyCount() >= EntitySpawnHelper.MaxEnemyCount)
                break;

            Vector3 spawnPos;
            if (!EntitySpawnHelper.TryFindRandomPosition(
                    player.position,
                    SpawnMinRadius,
                    SpawnMaxRadius,
                    spawnOccupied,
                    out spawnPos))
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(SpawnMinRadius, SpawnMaxRadius);
                spawnPos = player.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Debug.LogWarning("enemyManager: spawn spacing fallback");
            }

            spawnOccupied.Add(spawnPos);
            EntitySpawnHelper.Spawn(EnemyPath, spawnMode, spawnPos, Quaternion.identity, enemyPool, enemyPrefabHandle);
        }
    }

    #endregion
}
