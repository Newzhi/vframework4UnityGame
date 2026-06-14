using UnityEngine;

public enum BulletOwner
{
    Player,
    Enemy
}

/// <summary>
/// 池化弹丸：寿命或碰撞后通过 <see cref="PrefabPool.ReleaseObj"/> 归还，不调用 DestroyPool。
/// </summary>
public class Bullet : MonoBehaviour
{
    #region 游戏逻辑

    const float Speed = 28f;
    const float MaxLife = 4f;

    Rigidbody rb;
    PrefabPool ownerPool;
    BulletOwner owner;
    float spawnTime;
    bool released;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnEnable()
    {
        released = false;
        spawnTime = Time.time;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void Init(PrefabPool pool, BulletOwner bulletOwner)
    {
        ownerPool = pool;
        owner = bulletOwner;
        released = false;
        spawnTime = Time.time;
    }

    void Update()
    {
        if (released)
            return;

        transform.position += transform.forward * (Speed * Time.deltaTime);
        if (Time.time - spawnTime > MaxLife)
            ReturnToPool();
    }

    void OnTriggerEnter(Collider other)
    {
        if (released)
            return;

        if (owner == BulletOwner.Player)
        {
            enemyTest enemy = other.GetComponent<enemyTest>();
            if (enemy == null)
                return;

            enemy.GetDamage(10f);
            ReturnToPool();
            return;
        }

        PlayerTest player = other.GetComponent<PlayerTest>();
        if (player == null)
            return;

        player.GetDamage(8f);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (released)
            return;

        released = true;
        if (ownerPool != null)
            ownerPool.ReleaseObj(gameObject);
        else
            Destroy(gameObject);
    }

    #endregion
}
