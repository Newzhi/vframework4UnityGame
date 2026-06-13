using BaseFramework.BaseEventSys;
using UnityEngine;

public class enemyManager : MonoBehaviour
{
    const string EnemyPath = "Model/Prefabs/tester";
    const string BulletPath = "Model/Prefabs/Bullet";
    const float SpawnInterval = 2.5f;
    const float SpawnRadius = 18f;
    const int MaxActiveEnemies = 8;

    PrefabPool enemyPool;
    PrefabPool bulletPool;
    Transform enemiesRoot;
    Transform bulletsRoot;
    Transform player;
    float nextSpawnTime;

    void Start()
    {
        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("enemyManager: BundleResLoader.EnsureReady failed.");
            return;
        }

        player = GameObject.Find("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("enemyManager: Player not found.");
            return;
        }

        enemyPool = BundleResLoader.Instance.GetOrCreatPool(EnemyPath, maxInactiveCapacity: 12);
        bulletPool = BundleResLoader.Instance.GetOrCreatPool(BulletPath, maxInactiveCapacity: 48);
        enemiesRoot = BundleResLoader.Instance.GetOrCreateActivePoolRoot("Enemies");
        bulletsRoot = BundleResLoader.Instance.GetOrCreateActivePoolRoot("Bullets");
        nextSpawnTime = Time.time + 1f;
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
        logic.Init(enemyPool, player, bulletPool, bulletsRoot);

        GameEventBus.SentEvent(new EnemySpawnedEvent { Enemy = enemy });
    }
}
