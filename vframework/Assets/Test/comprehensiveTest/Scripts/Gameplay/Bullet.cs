using UnityEngine;

public enum BulletOwner
{
    Player,
    Enemy,
    Ally
}

/// <summary>
/// 池化弹丸：OnEnable 自查子弹池；发射方仅需 <see cref="SetOwner"/>；归还走 <see cref="RecycleObj"/>。
/// </summary>
public class Bullet : MonoBehaviour
{
    #region 游戏逻辑

    const string BulletPath = "Model/Prefabs/Bullet";
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

        if (ownerPool == null)
            PrefabPoolManager.Instance.TryGetPool(BulletPath, out ownerPool);
    }

    /// <summary>发射方在 GetObj 后设置归属（玩家弹 / 敌人弹）。</summary>
    public void SetOwner(BulletOwner bulletOwner)
    {
        owner = bulletOwner;
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

        if (owner == BulletOwner.Player || owner == BulletOwner.Ally)
        {
            enemyTest enemy = other.GetComponent<enemyTest>();
            if (enemy == null)
                return;

            enemy.GetDamage(owner == BulletOwner.Player ? 10f : 9f);
            ReturnToPool();
            return;
        }

        AllyTest ally = other.GetComponent<AllyTest>();
        if (ally != null)
        {
            ally.GetDamage(8f);
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
            ownerPool.RecycleObj(gameObject);
        else
            PrefabPoolManager.Instance.RecycleObj(gameObject, BulletPath);
    }

    #endregion
}
