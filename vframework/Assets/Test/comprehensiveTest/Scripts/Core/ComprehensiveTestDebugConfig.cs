using UnityEngine;

/// <summary>
/// 综合测试场景调试项：Inspector 挂载在 PoolTest 根节点上切换敌人生成方式。
/// </summary>
public class ComprehensiveTestDebugConfig : MonoBehaviour
{
    public enum EnemySpawnMode
    {
        /// <summary>敌人 PrefabPool GetObj / RecycleObj（死亡不 Destroy，子弹 ref 不因死亡下降）。</summary>
        Pooled = 0,

        /// <summary>Load + Instantiate / 死亡 Destroy（OnDestroy 卸子弹池份额，便于验 refCount）。</summary>
        DirectInstantiate = 1
    }

    public static ComprehensiveTestDebugConfig Instance { get; private set; }

    [SerializeField] EnemySpawnMode enemySpawnMode = EnemySpawnMode.DirectInstantiate;

    public EnemySpawnMode CurrentEnemySpawnMode => enemySpawnMode;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static EnemySpawnMode ResolveEnemySpawnMode()
    {
        return Instance != null ? Instance.CurrentEnemySpawnMode : EnemySpawnMode.Pooled;
    }
}
