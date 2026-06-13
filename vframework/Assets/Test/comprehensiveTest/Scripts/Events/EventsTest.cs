using BaseFramework.BaseEventSys;
using UnityEngine;

public struct PlayerShotEvent : IGameEvent
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public struct DamageTakenEvent : IGameEvent
{
    public GameObject Target;
    public float Amount;
    public bool IsPlayer;
}

public struct EntityDeadEvent : IGameEvent
{
    public GameObject Entity;
    public bool IsPlayer;
    public int RemainingLives;
}

public struct EnemySpawnedEvent : IGameEvent
{
    public GameObject Enemy;
}

public struct EnemyShotEvent : IGameEvent
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public struct PlayerRespawnedEvent : IGameEvent
{
    public Vector3 Position;
    public int RemainingLives;
}

public struct PlayerDamageEvent : IGameEvent
{
    public float Amount;
}
