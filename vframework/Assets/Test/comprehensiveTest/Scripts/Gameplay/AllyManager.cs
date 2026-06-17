using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 友军生成：与敌人相同间隔刷出，上限共用 <see cref="EntitySpawnHelper.MaxEntityCount"/>。
/// </summary>
public class AllyManager : MonoBehaviour
{
    const string AllyPath = "Model/Prefabs/Ally";
    const float SpawnInterval = 2.5f;
    const int SpawnBatchSize = 2;
    const float SpawnMinRadius = 4f;
    const float SpawnMaxRadius = 11f;

    [SerializeField] int maxAllyCount = EntitySpawnHelper.MaxEntityCount;

    PrefabPool allyPool;
    IAssetHandle allyPrefabHandle;
    Transform player;
    float nextSpawnTime;
    ComprehensiveTestDebugConfig.EntitySpawnMode spawnMode;
    readonly List<Vector3> spawnOccupied = new List<Vector3>();

    void Start()
    {
        spawnMode = ComprehensiveTestDebugConfig.ResolveEntitySpawnMode();

        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("AllyManager: EnsureReady failed.");
            return;
        }

        player = GameObject.Find("Player")?.transform;
        if (player == null)
            Debug.LogError("AllyManager: Player not found.");

        int maxInactive = Mathf.Max(EntitySpawnHelper.MaxEntityCount, GetMaxAllyCount());
        if (EntitySpawnHelper.IsPooled(spawnMode))
            allyPool = PrefabPoolManager.Instance.GetOrCreatPool(AllyPath, maxInactiveCapacity: maxInactive);
        else if (!EntitySpawnHelper.IsAutoUnload(spawnMode))
            allyPrefabHandle = BundleResLoader.Instance.Load<GameObject>(AllyPath);

        nextSpawnTime = Time.time + 0.8f;
    }

    void OnDestroy()
    {
        if (EntitySpawnHelper.IsPooled(spawnMode))
            PrefabPoolManager.Instance.ReleasePoolShare(AllyPath);
        else if (!EntitySpawnHelper.IsAutoUnload(spawnMode))
        {
            allyPrefabHandle?.Release();
            allyPrefabHandle = null;
        }
    }

    void Update()
    {
        if (player == null || Time.time < nextSpawnTime)
            return;

        if (GetActiveAllyCount() >= GetMaxAllyCount())
            return;

        if (EntitySpawnHelper.IsPooled(spawnMode) && allyPool == null)
            return;

        if (!EntitySpawnHelper.IsPooled(spawnMode) && !EntitySpawnHelper.IsAutoUnload(spawnMode)
            && allyPrefabHandle == null)
            return;

        SpawnAllyBatch();
        nextSpawnTime = Time.time + SpawnInterval;
    }

    int GetMaxAllyCount()
    {
        return Mathf.Clamp(maxAllyCount, 0, EntitySpawnHelper.MaxEntityCount);
    }

    int GetActiveAllyCount()
    {
        if (EntitySpawnHelper.IsPooled(spawnMode))
            return allyPool != null ? allyPool.ActiveCount : 0;

        return AllyTest.DirectAliveCount;
    }

    void SpawnAllyBatch()
    {
        spawnOccupied.Clear();
        EntitySpawnHelper.CollectOccupiedPositions(spawnOccupied);

        for (int i = 0; i < SpawnBatchSize; i++)
        {
            if (GetActiveAllyCount() >= GetMaxAllyCount())
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
                Debug.LogWarning("AllyManager: spawn spacing fallback");
            }

            spawnOccupied.Add(spawnPos);
            EntitySpawnHelper.Spawn(AllyPath, spawnMode, spawnPos, Quaternion.identity, allyPool, allyPrefabHandle);
        }
    }
}
