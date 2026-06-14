using BaseFramework.BaseEventSys;
using UnityEngine;

public class enemyManager : MonoBehaviour
{
    const string EnemyPath = "Model/Prefabs/tester";
    const int EnemyMaxInactive = 12;
    const float SpawnInterval = 2.5f;
    const float SpawnRadius = 18f;
    const int MaxActiveEnemies = 8;

    PrefabPool enemyPool;
    Transform enemiesRoot;
    Transform player;
    float nextSpawnTime;

    void Start()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("enemyManager: EnsureReady failed.");
            return;
        }

        enemyPool = BundleResLoader.Instance.GetOrCreatPool(EnemyPath, maxInactiveCapacity: EnemyMaxInactive);
        enemiesRoot = BundleResLoader.Instance.GetOrCreateActivePoolRoot("Enemies");

        player = GameObject.Find("Player")?.transform;
        if (player == null)
            Debug.LogError("enemyManager: Player not found.");

        nextSpawnTime = Time.time + 1f;
    }

    void OnDestroy()
    {
        ReleaseOwnedPool(EnemyPath, enemyPool);
    }

    static void ReleaseOwnedPool(string loadPath, PrefabPool pool)
    {
        if (pool != null && pool.IsPoolCreated)
            pool.DestroyPool();

        BundleResLoader.Instance.DestroyPoolByLoadPath(loadPath);
    }

    void Update()
    {
        if (enemyPool == null || player == null || Time.time < nextSpawnTime)
            return;

        if (enemyPool.ActiveCount >= MaxActiveEnemies)
            return;

        SpawnEnemy();
        nextSpawnTime = Time.time + SpawnInterval;
    }

    void SpawnEnemy()
    {
        Vector3 offset = Random.insideUnitSphere * SpawnRadius;
        offset.y = 0f;
        Vector3 spawnPos = player.position + offset;

        GameObject enemy = enemyPool.GetObj(spawnPos, Quaternion.identity, enemiesRoot);
        if (enemy == null)
            return;

        enemyTest logic = enemy.GetComponent<enemyTest>() ?? enemy.AddComponent<enemyTest>();
        logic.Init(enemyPool, player);

        GameEventBus.SentEvent(new EnemySpawnedEvent { Enemy = enemy });
    }
}
