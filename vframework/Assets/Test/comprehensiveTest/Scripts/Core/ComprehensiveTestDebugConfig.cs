using UnityEngine;

/// <summary>
/// 综合测试场景调试项：Inspector 挂载在 PoolTest 根节点，敌人/友军共用生成与卸载策略。
/// </summary>
public class ComprehensiveTestDebugConfig : MonoBehaviour
{
    public enum EntitySpawnMode
    {
        /// <summary>PrefabPool GetObj / RecycleObj（死亡不 Destroy，子弹 ref 不因死亡下降）。</summary>
        Pooled = 0,

        /// <summary>Load + Instantiate / 死亡 Destroy（OnDestroy 卸子弹池份额；句柄由 Manager 持有）。</summary>
        DirectInstantiate = 1,

        /// <summary>LoadGameObject + AssetReference；死亡 Destroy 自动 Release 本次 prefab 句柄。</summary>
        AutoUnload = 2
    }

    public static ComprehensiveTestDebugConfig Instance { get; private set; }

    [SerializeField] EntitySpawnMode enemySpawnMode = EntitySpawnMode.DirectInstantiate;

    public EntitySpawnMode CurrentEntitySpawnMode => enemySpawnMode;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static EntitySpawnMode ResolveEntitySpawnMode()
    {
        return Instance != null ? Instance.CurrentEntitySpawnMode : EntitySpawnMode.Pooled;
    }
}
