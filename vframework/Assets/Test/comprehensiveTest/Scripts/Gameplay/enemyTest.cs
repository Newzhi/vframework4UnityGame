using BaseFramework.BaseEventSys;
using UnityEngine;

/// <summary>
/// 敌人 AI（挂在 enemy 预制体）：自行初始化、首次射击建子弹池、死亡/销毁时对称卸池。
/// </summary>
public class enemyTest : MonoBehaviour
{
    #region 游戏逻辑

    enum EnemyState
    {
        Chase,
        Shoot
    }

    const string BulletPath = "Model/Prefabs/Bullet";
    const string EnemyPath = "Model/Prefabs/enemy";
    const int BulletMaxInactive = 48;
    const float ShootRange = 12f;
    const float ShootRangeSqr = ShootRange * ShootRange;
    const float FireCooldown = 1.1f;
    const float FireForwardOffset = 1.2f;

    float moveSpeed = 5f;
    float hp = 30f;
    EnemyState state;
    PrefabPool ownerPool;
    PrefabPool bulletPool;
    bool ownsBulletPoolShare;
    bool pooledEnemyLife;
    Transform target;
    float nextShootTime;

    void OnEnable()
    {
        BootstrapGameplay();
        BootstrapTest();
    }

    void OnDestroy()
    {
        TrackDirectAliveOnDestroy();
        ReleaseBulletPoolShare();
    }

    void BootstrapGameplay()
    {
        target = GameObject.Find("Player")?.transform;
        hp = 30f;
        state = EnemyState.Chase;
        nextShootTime = 0f;

        pooledEnemyLife = IsPooledSpawnMode();
        if (pooledEnemyLife)
        {
            ownerPool = null;
            if (BundleResLoader.Instance.TryGetPool(EnemyPath, out PrefabPool pool))
                ownerPool = pool;
            else
                Debug.LogError("enemyTest: enemy pool not found, path=" + EnemyPath);
        }
        else
        {
            ownerPool = null;
            bulletPool = null;
            ownsBulletPoolShare = false;
        }
    }

    void EnsureBulletPool()
    {
        if (bulletPool != null)
            return;

        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("enemyTest: EnsureReady failed.");
            return;
        }

        bulletPool = BundleResLoader.Instance.GetOrCreatPool(BulletPath, maxInactiveCapacity: BulletMaxInactive);
        if (bulletPool == null)
            return;

        ownsBulletPoolShare = true;
        TrackBulletPoolShareAcquired();
    }

    void ReleaseBulletPoolShare()
    {
        if (!ownsBulletPoolShare)
            return;

        LogBulletPoolReleaseBefore();
        BundleResLoader.Instance.DestroyPoolByLoadPath(BulletPath);
        ownsBulletPoolShare = false;
        bulletPool = null;
        TrackBulletPoolShareReleased();
        LogBulletPoolReleaseAfter();
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;
        float distSqr = toPlayer.sqrMagnitude;

        state = distSqr <= ShootRangeSqr ? EnemyState.Shoot : EnemyState.Chase;

        switch (state)
        {
            case EnemyState.Chase:
                Chase(toPlayer);
                break;
            case EnemyState.Shoot:
                Aim(toPlayer);
                TryShoot();
                break;
        }
    }

    void Chase(Vector3 toPlayer)
    {
        if (toPlayer.sqrMagnitude < 0.01f)
            return;

        Vector3 dir = toPlayer.normalized;
        transform.position += dir * (moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(dir);
    }

    void Aim(Vector3 toPlayer)
    {
        if (toPlayer.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
    }

    void TryShoot()
    {
        EnsureBulletPool();
        if (bulletPool == null || Time.time < nextShootTime)
            return;

        nextShootTime = Time.time + FireCooldown;

        Vector3 firePos = transform.position + transform.forward * FireForwardOffset + Vector3.up * 0.6f;
        Quaternion fireRot = transform.rotation;

        GameObject bulletGo = bulletPool.GetObj(firePos, fireRot);
        if (bulletGo == null)
            return;

        bulletGo.GetComponent<Bullet>()?.Init(bulletPool, BulletOwner.Enemy);
        EmitEnemyShotEvent(firePos, fireRot);
    }

    public void GetDamage(float amount)
    {
        if (hp <= 0f)
            return;

        hp -= amount;
        EmitDamageTakenEvent(amount);

        if (hp <= 0f)
            Dead();
    }

    void Dead()
    {
        EmitEntityDeadEvent();

        if (pooledEnemyLife && ownerPool != null)
        {
            ownerPool.ReleaseObj(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    #endregion

    #region 综合测试

    /// <summary>当前持有子弹池份额的敌人实例数（供 Logger 校验 refCount）。</summary>
    public static int BulletPoolShareCount { get; private set; }

    /// <summary>DirectInstantiate 模式下场上存活敌人数（供 enemyManager 限流）。</summary>
    public static int DirectAliveCount { get; private set; }

    static bool IsPooledSpawnMode()
    {
        return ComprehensiveTestDebugConfig.ResolveEnemySpawnMode()
            == ComprehensiveTestDebugConfig.EnemySpawnMode.Pooled;
    }

    void BootstrapTest()
    {
        if (!pooledEnemyLife)
            DirectAliveCount++;

        GameEventBus.SentEvent(new EnemySpawnedEvent { Enemy = gameObject });
    }

    void TrackDirectAliveOnDestroy()
    {
        if (!pooledEnemyLife && DirectAliveCount > 0)
            DirectAliveCount--;
    }

    void TrackBulletPoolShareAcquired()
    {
        BulletPoolShareCount++;
        ComprehensiveTestLogger.LogBulletPoolRef("敌人GetOrCreatPool#" + gameObject.GetInstanceID());
    }

    void TrackBulletPoolShareReleased()
    {
        BulletPoolShareCount--;
    }

    void LogBulletPoolReleaseBefore()
    {
        ComprehensiveTestLogger.LogBulletPoolRef("敌人OnDestroy释池前#" + gameObject.GetInstanceID());
    }

    void LogBulletPoolReleaseAfter()
    {
        ComprehensiveTestLogger.LogBulletPoolRef("敌人OnDestroy释池后#" + gameObject.GetInstanceID());
    }

    void EmitEnemyShotEvent(Vector3 firePos, Quaternion fireRot)
    {
        GameEventBus.SentEvent(new EnemyShotEvent { Position = firePos, Rotation = fireRot });
    }

    void EmitDamageTakenEvent(float amount)
    {
        GameEventBus.SentEvent(new DamageTakenEvent
        {
            Target = gameObject,
            Amount = amount,
            IsPlayer = false
        });
    }

    void EmitEntityDeadEvent()
    {
        GameEventBus.SentEvent(new EntityDeadEvent { Entity = gameObject, IsPlayer = false });
    }

    #endregion
}
