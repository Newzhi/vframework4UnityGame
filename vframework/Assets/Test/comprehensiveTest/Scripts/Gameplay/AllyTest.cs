using System.Collections.Generic;
using BaseFramework.BaseEventSys;
using UnityEngine;

/// <summary>
/// 友军 AI：跟随玩家（保持间距）、自动射击附近敌人；生命周期与敌人共用 EntitySpawnMode。
/// </summary>
public class AllyTest : MonoBehaviour
{
    enum AllyState
    {
        Follow,
        Combat
    }

    const string BulletPath = "Model/Prefabs/Bullet";
    const string AllyPath = "Model/Prefabs/Ally";
    const int BulletMaxInactive = 48;
    const float FollowMinDist = 3.5f;
    const float FollowMaxDist = 7f;
    const float ShootRange = 11f;
    const float ShootRangeSqr = ShootRange * ShootRange;
    const float FireCooldown = 0.95f;
    const float FireForwardOffset = 1.1f;
    const float MoveSpeed = 6f;
    const float MaxHp = 240f;

    float moveSpeed = MoveSpeed;
    float hp;
    AllyState state;
    PrefabPool ownerPool;
    PrefabPool bulletPool;
    bool ownsBulletPoolShare;
    bool pooledLife;
    Transform player;
    enemyTest combatTarget;
    float nextShootTime;

    static readonly List<AllyTest> ActiveInstances = new List<AllyTest>();

    public static int ActiveInstanceCount => ActiveInstances.Count;

    public static AllyTest GetActiveInstanceAt(int index)
    {
        return index >= 0 && index < ActiveInstances.Count ? ActiveInstances[index] : null;
    }

    public static void CollectActivePositions(List<Vector3> positions)
    {
        for (int i = 0; i < ActiveInstances.Count; i++)
        {
            AllyTest ally = ActiveInstances[i];
            if (ally == null || !ally.gameObject.activeInHierarchy)
                continue;

            positions.Add(ally.transform.position);
        }
    }

    void OnEnable()
    {
        ActiveInstances.Add(this);
        BootstrapGameplay();
        BootstrapTest();
    }

    void OnDisable()
    {
        ActiveInstances.Remove(this);
    }

    void OnDestroy()
    {
        TrackDirectAliveOnDestroy();
        ReleaseBulletPoolShare();
    }

    void BootstrapGameplay()
    {
        player = GameObject.Find("Player")?.transform;
        state = AllyState.Follow;
        combatTarget = null;
        nextShootTime = 0f;
        hp = MaxHp;

        pooledLife = EntitySpawnHelper.IsPooled(ComprehensiveTestDebugConfig.ResolveEntitySpawnMode());
        if (pooledLife)
        {
            ownerPool = null;
            if (PrefabPoolManager.Instance.TryGetPool(AllyPath, out PrefabPool pool))
                ownerPool = pool;
            else
                Debug.LogError("AllyTest: ally pool not found, path=" + AllyPath);
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
            Debug.LogError("AllyTest: EnsureReady failed.");
            return;
        }

        bulletPool = PrefabPoolManager.Instance.GetOrCreatPool(BulletPath, maxInactiveCapacity: BulletMaxInactive);
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
        PrefabPoolManager.Instance.ReleasePoolShare(BulletPath);
        ownsBulletPoolShare = false;
        bulletPool = null;
        TrackBulletPoolShareReleased();
        LogBulletPoolReleaseAfter();
    }

    void Update()
    {
        if (player == null)
            return;

        RefreshCombatTarget();
        state = combatTarget != null ? AllyState.Combat : AllyState.Follow;

        switch (state)
        {
            case AllyState.Follow:
                FollowPlayer();
                break;
            case AllyState.Combat:
                Combat();
                break;
        }
    }

    void RefreshCombatTarget()
    {
        if (combatTarget != null && combatTarget.gameObject.activeInHierarchy)
        {
            Vector3 toEnemy = combatTarget.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude <= ShootRangeSqr)
                return;

            combatTarget = null;
        }

        enemyTest.TryGetNearest(transform.position, ShootRange, out combatTarget, out _);
    }

    void FollowPlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > FollowMaxDist)
            MoveAlong(toPlayer, dist);
        else if (dist < FollowMinDist && dist > 0.01f)
            MoveAlong(-toPlayer, dist);
        else if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
    }

    void Combat()
    {
        if (combatTarget == null)
            return;

        Vector3 toEnemy = combatTarget.transform.position - transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(toEnemy.normalized);
        TryShoot();
    }

    void MoveAlong(Vector3 direction, float dist)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        EntitySpawnHelper.MoveWithAvoidance(transform, direction, moveSpeed);
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

        bulletGo.GetComponent<Bullet>()?.SetOwner(BulletOwner.Ally);
        EmitAllyShotEvent(firePos, fireRot);
    }

    void Dead()
    {
        EmitEntityDeadEvent();

        if (pooledLife && ownerPool != null)
        {
            ownerPool.RecycleObj(gameObject);
            return;
        }

        Destroy(gameObject);
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

    void EmitDamageTakenEvent(float amount)
    {
        GameEventBus.SentEvent(new DamageTakenEvent
        {
            Target = gameObject,
            Amount = amount,
            IsPlayer = false
        });
    }

    #region 综合测试

    public static int BulletPoolShareCount { get; private set; }
    public static int DirectAliveCount { get; private set; }

    void BootstrapTest()
    {
        if (!pooledLife)
            DirectAliveCount++;

        GameEventBus.SentEvent(new AllySpawnedEvent { Ally = gameObject });
    }

    void TrackDirectAliveOnDestroy()
    {
        if (!pooledLife && DirectAliveCount > 0)
            DirectAliveCount--;
    }

    void TrackBulletPoolShareAcquired()
    {
        BulletPoolShareCount++;
        ComprehensiveTestLogger.LogBulletPoolRef("友军GetOrCreatPool#" + gameObject.GetInstanceID());
    }

    void TrackBulletPoolShareReleased()
    {
        BulletPoolShareCount--;
    }

    void LogBulletPoolReleaseBefore()
    {
        ComprehensiveTestLogger.LogBulletPoolRef("友军OnDestroy释池前#" + gameObject.GetInstanceID());
    }

    void LogBulletPoolReleaseAfter()
    {
        ComprehensiveTestLogger.LogBulletPoolRef("友军OnDestroy释池后#" + gameObject.GetInstanceID());
    }

    void EmitAllyShotEvent(Vector3 firePos, Quaternion fireRot)
    {
        GameEventBus.SentEvent(new AllyShotEvent { Position = firePos, Rotation = fireRot });
    }

    void EmitEntityDeadEvent()
    {
        GameEventBus.SentEvent(new EntityDeadEvent { Entity = gameObject, IsPlayer = false });
    }

    #endregion
}
