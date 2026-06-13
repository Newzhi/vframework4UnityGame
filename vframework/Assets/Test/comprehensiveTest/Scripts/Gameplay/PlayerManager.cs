using BaseFramework.BaseEventSys;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家生命与复活：3 条命，血量归零后在最近死亡点复活；命尽返回开始场景。
/// </summary>
public class PlayerManager : MonoBehaviour
{
    const int MaxLives = 3;
    const float MaxHp = 500f;
    const float RespawnLockDuration = 1.2f;

    public static PlayerManager Instance { get; private set; }

    [SerializeField] Text statusText;
    [SerializeField] Transform player;

    int lives;
    float hp;
    float respawnLockUntil;
    Vector3 lastDeathPosition;
    Quaternion lastDeathRotation;
    IPlayerGameplay playerGameplay;

    public int Lives => lives;
    public float Hp => hp;
    public float MaxHpValue => MaxHp;

    void Awake()
    {
        Instance = this;

        if (statusText == null)
            statusText = FindPlayerStatusText();

        if (player == null)
            player = GameObject.Find("Player")?.transform;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        GameEventBus.RegisterEvent<PlayerDamageEvent>(OnPlayerDamage);
    }

    void OnDisable()
    {
        GameEventBus.DeRegisterEvent<PlayerDamageEvent>(OnPlayerDamage);
    }

    void Start()
    {
        playerGameplay = player != null ? player.GetComponent<IPlayerGameplay>() : null;
        lives = MaxLives;
        hp = MaxHp;

        if (player != null)
        {
            lastDeathPosition = player.position;
            lastDeathRotation = player.rotation;
        }

        RefreshStatusText();
    }

    void OnPlayerDamage(PlayerDamageEvent e)
    {
        ApplyDamage(e.Amount);
    }

    public void ApplyDamage(float amount)
    {
        if (player == null || Time.time < respawnLockUntil || hp <= 0f)
            return;

        hp -= amount;
        GameEventBus.SentEvent(new DamageTakenEvent
        {
            Target = player.gameObject,
            Amount = amount,
            IsPlayer = true
        });

        RefreshStatusText();

        if (hp <= 0f)
            OnHpDepleted();
    }

    void OnHpDepleted()
    {
        lastDeathPosition = player.position;
        lastDeathRotation = player.rotation;

        if (playerGameplay != null)
            playerGameplay.SetGameplayEnabled(false);

        lives--;
        GameEventBus.SentEvent(new EntityDeadEvent
        {
            Entity = player.gameObject,
            IsPlayer = true,
            RemainingLives = lives
        });

        if (lives <= 0)
        {
            ComprehensiveTestSceneFlow.ReturnToStartScene();
            return;
        }

        RespawnAtLastDeathPoint();
    }

    void RespawnAtLastDeathPoint()
    {
        hp = MaxHp;
        respawnLockUntil = Time.time + RespawnLockDuration;
        player.SetPositionAndRotation(lastDeathPosition, lastDeathRotation);

        if (playerGameplay != null)
            playerGameplay.SetGameplayEnabled(true);

        RefreshStatusText();
        GameEventBus.SentEvent(new PlayerRespawnedEvent
        {
            Position = lastDeathPosition,
            RemainingLives = lives
        });
    }

    public void RefreshStatusText()
    {
        if (statusText == null)
            return;

        statusText.text = $"HP {Mathf.CeilToInt(hp)}/{MaxHp:F0}  Lives {lives}/{MaxLives}";
    }

    static Text FindPlayerStatusText()
    {
        Text[] texts = FindObjectsOfType<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text.gameObject.name == "Log")
                continue;

            Transform parent = text.transform.parent;
            if (parent != null && parent.name.Trim() == "ExitGame")
                continue;

            return text;
        }

        return null;
    }
}
