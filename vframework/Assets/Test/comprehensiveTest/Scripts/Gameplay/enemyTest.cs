using BaseFramework.BaseEventSys;
using UnityEngine;

public class enemyTest : MonoBehaviour
{
    enum EnemyState
    {
        Chase,
        Shoot
    }

    const string BulletPath = "Model/Prefabs/Bullet";
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
    Transform bulletsRoot;
    Transform target;
    float nextShootTime;

    public void Init(PrefabPool pool, Transform playerTarget)
    {
        ownerPool = pool;
        target = playerTarget;
        hp = 30f;
        state = EnemyState.Chase;
        nextShootTime = 0f;
        bulletPool = null;
        bulletsRoot = null;
    }

    void EnsureBulletPool()
    {
        if (bulletPool != null)
            return;

        if (BundleResLoader.Instance.TryGetPool(BulletPath, out bulletPool))
        {
            bulletsRoot = BundleResLoader.Instance.GetOrCreateActivePoolRoot("Bullets");
            return;
        }

        bulletPool = BundleResLoader.Instance.GetOrCreatPool(BulletPath, maxInactiveCapacity: BulletMaxInactive);
        bulletsRoot = BundleResLoader.Instance.GetOrCreateActivePoolRoot("Bullets");
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
        transform.position += dir * moveSpeed * Time.deltaTime;
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

        GameObject bulletGo = bulletPool.GetObj(firePos, fireRot, bulletsRoot);
        if (bulletGo == null)
            return;

        bulletGo.GetComponent<Bullet>()?.Init(bulletPool, BulletOwner.Enemy);
        GameEventBus.SentEvent(new EnemyShotEvent { Position = firePos, Rotation = fireRot });
    }

    public void GetDamage(float amount)
    {
        if (hp <= 0f)
            return;

        hp -= amount;
        GameEventBus.SentEvent(new DamageTakenEvent
        {
            Target = gameObject,
            Amount = amount,
            IsPlayer = false
        });

        if (hp <= 0f)
            Dead();
    }

    void Dead()
    {
        GameEventBus.SentEvent(new EntityDeadEvent { Entity = gameObject, IsPlayer = false });
        ownerPool.ReleaseObj(gameObject);
    }
}
