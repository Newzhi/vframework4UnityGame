using BaseFramework.BaseEventSys;
using UnityEngine;

public class PlayerTest : MonoBehaviour, IPlayerGameplay
{
    const string BulletPath = "Model/Prefabs/Bullet";
    const float MoveSpeed = 14f;
    const float FireCooldown = 0.12f;
    const float FireForwardOffset = 1.4f;
    const float AimHeight = 0.6f;

    float nextFireTime;
    PrefabPool bulletPool;
    Transform bulletsRoot;
    Camera mainCamera;
    bool gameplayEnabled = true;

    void Start()
    {
        mainCamera = Camera.main;

        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("PlayerTest: BundleResLoader.EnsureReady failed.");
            return;
        }

        bulletPool = BundleResLoader.Instance.GetOrCreatPool(BulletPath, maxInactiveCapacity: 48);
        bulletsRoot = BundleResLoader.Instance.GetOrCreateActivePoolRoot("Bullets");
    }

    public void SetGameplayEnabled(bool enabled)
    {
        gameplayEnabled = enabled;
    }

    void Update()
    {
        if (!gameplayEnabled)
            return;

        UpdateAimRotation();
        Move();
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
            Shot();
    }

    void UpdateAimRotation()
    {
        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(aimDir);
    }

    void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (horizontal == 0f && vertical == 0f)
            return;

        Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;
        transform.position += dir * (MoveSpeed * Time.deltaTime);
    }

    void Shot()
    {
        if (bulletPool == null || Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + FireCooldown;

        Vector3 aimDir = GetAimDirection();
        if (aimDir.sqrMagnitude < 0.0001f)
            aimDir = transform.forward;

        Quaternion fireRot = Quaternion.LookRotation(aimDir);
        Vector3 firePos = transform.position + aimDir * FireForwardOffset + Vector3.up * AimHeight;

        GameObject bulletGo = bulletPool.GetObj(firePos, fireRot, bulletsRoot);
        if (bulletGo == null)
            return;

        bulletGo.GetComponent<Bullet>()?.Init(bulletPool, BulletOwner.Player);
        GameEventBus.SentEvent(new PlayerShotEvent { Position = firePos, Rotation = fireRot });
    }

    Vector3 GetAimDirection()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return transform.forward;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (!groundPlane.Raycast(ray, out float enter))
            return transform.forward;

        Vector3 worldPoint = ray.GetPoint(enter);
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
    }

    public void GetDamage(float amount)
    {
        GameEventBus.SentEvent(new PlayerDamageEvent { Amount = amount });
    }
}
