using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人/友军统一生成：池化、Load+Instantiate、LoadGameObject（AssetReference 自动卸句柄）及站位间距。
/// </summary>
public static class EntitySpawnHelper
{
    public const int MaxEntityCount = 36;
    public const int MaxEnemyCount = 44;
    public const float MinSeparation = 2.8f;

    const float MinSeparationSqr = MinSeparation * MinSeparation;
    const int RandomSpawnAttempts = 16;

    public static bool IsPooled(ComprehensiveTestDebugConfig.EntitySpawnMode mode)
    {
        return mode == ComprehensiveTestDebugConfig.EntitySpawnMode.Pooled;
    }

    public static bool IsAutoUnload(ComprehensiveTestDebugConfig.EntitySpawnMode mode)
    {
        return mode == ComprehensiveTestDebugConfig.EntitySpawnMode.AutoUnload;
    }

    public static bool UsesDestroyOnDeath(ComprehensiveTestDebugConfig.EntitySpawnMode mode)
    {
        return !IsPooled(mode);
    }

    public static void Spawn(
        string loadPath,
        ComprehensiveTestDebugConfig.EntitySpawnMode mode,
        Vector3 position,
        Quaternion rotation,
        PrefabPool pool,
        IAssetHandle sharedHandle)
    {
        switch (mode)
        {
            case ComprehensiveTestDebugConfig.EntitySpawnMode.Pooled:
                pool?.GetObj(position, rotation);
                break;

            case ComprehensiveTestDebugConfig.EntitySpawnMode.DirectInstantiate:
                sharedHandle?.InstantiateAt(position, rotation, null);
                break;

            case ComprehensiveTestDebugConfig.EntitySpawnMode.AutoUnload:
                BundleResLoader.Instance.LoadGameObject(loadPath, position, rotation, null);
                break;
        }
    }

    public static void CollectOccupiedPositions(List<Vector3> positions)
    {
        enemyTest.CollectActivePositions(positions);
        AllyTest.CollectActivePositions(positions);
    }

    public static bool IsSeparated(Vector3 candidate, List<Vector3> occupied)
    {
        for (int i = 0; i < occupied.Count; i++)
        {
            Vector3 delta = candidate - occupied[i];
            delta.y = 0f;
            if (delta.sqrMagnitude < MinSeparationSqr)
                return false;
        }

        return true;
    }

    /// <summary>环上均匀分布，半径随数量放大以保证弧长间距。</summary>
    public static bool TryFindRingPosition(
        Vector3 center,
        int index,
        int total,
        float baseRadius,
        List<Vector3> occupied,
        out Vector3 position)
    {
        float minRadius = Mathf.Max(baseRadius, (MinSeparation * total) / (2f * Mathf.PI) + 0.5f);
        float angle = (360f / total) * index + 90f;

        for (int ring = 0; ring < 4; ring++)
        {
            float radius = minRadius + ring * MinSeparation;
            for (int jitter = 0; jitter < 8; jitter++)
            {
                float jitterAngle = angle + jitter * (360f / total / 2f);
                Vector3 offset = Quaternion.Euler(0f, jitterAngle, 0f) * Vector3.back * radius;
                offset.y = 0f;
                Vector3 candidate = center + offset;
                if (IsSeparated(candidate, occupied))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        Vector3 fallbackOffset = Quaternion.Euler(0f, angle, 0f) * Vector3.back * minRadius;
        fallbackOffset.y = 0f;
        position = center + fallbackOffset;
        return false;
    }

    /// <summary>在环形区域内随机找点，避开已占用位置。</summary>
    public static bool TryFindRandomPosition(
        Vector3 center,
        float minRadius,
        float maxRadius,
        List<Vector3> occupied,
        out Vector3 position)
    {
        for (int attempt = 0; attempt < RandomSpawnAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minRadius, maxRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 candidate = center + offset;
            if (IsSeparated(candidate, occupied))
            {
                position = candidate;
                return true;
            }
        }

        position = center;
        return false;
    }

    #region 移动避障

    public const float AvoidRadius = 3f;
    const float AvoidRadiusSqr = AvoidRadius * AvoidRadius;
    const float AvoidanceSeparationWeight = 1.6f;

    public static void MoveWithAvoidance(Transform unit, Vector3 desiredDirection, float speed)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f)
            return;

        Vector3 dir = BlendAvoidanceDirection(desiredDirection, unit.position, unit);
        unit.position += dir * (speed * Time.deltaTime);
        unit.rotation = Quaternion.LookRotation(dir);
    }

    public static Vector3 BlendAvoidanceDirection(Vector3 desiredDirection, Vector3 position, Transform skip)
    {
        if (desiredDirection.sqrMagnitude < 0.0001f)
            return desiredDirection;

        Vector3 separation = ComputeAvoidanceSeparation(position, skip);
        Vector3 blended = desiredDirection.normalized + separation * AvoidanceSeparationWeight;
        blended.y = 0f;
        if (blended.sqrMagnitude < 0.0001f)
            return desiredDirection.normalized;

        return blended.normalized;
    }

    public static Vector3 ComputeAvoidanceSeparation(Vector3 position, Transform skip)
    {
        Vector3 force = Vector3.zero;
        int count = 0;
        AccumulateEnemyAvoidance(position, skip, ref force, ref count);
        AccumulateAllyAvoidance(position, skip, ref force, ref count);
        return count > 0 ? force / count : Vector3.zero;
    }

    static void AccumulateEnemyAvoidance(Vector3 position, Transform skip, ref Vector3 force, ref int count)
    {
        for (int i = 0; i < enemyTest.ActiveInstanceCount; i++)
        {
            enemyTest enemy = enemyTest.GetActiveInstanceAt(i);
            if (enemy == null || enemy.transform == skip || !enemy.gameObject.activeInHierarchy)
                continue;

            AccumulateAvoidanceForce(position, enemy.transform.position, ref force, ref count);
        }
    }

    static void AccumulateAllyAvoidance(Vector3 position, Transform skip, ref Vector3 force, ref int count)
    {
        for (int i = 0; i < AllyTest.ActiveInstanceCount; i++)
        {
            AllyTest ally = AllyTest.GetActiveInstanceAt(i);
            if (ally == null || ally.transform == skip || !ally.gameObject.activeInHierarchy)
                continue;

            AccumulateAvoidanceForce(position, ally.transform.position, ref force, ref count);
        }
    }

    static void AccumulateAvoidanceForce(Vector3 position, Vector3 otherPosition, ref Vector3 force, ref int count)
    {
        Vector3 away = position - otherPosition;
        away.y = 0f;
        float distSqr = away.sqrMagnitude;
        if (distSqr >= AvoidRadiusSqr || distSqr < 0.0001f)
            return;

        float dist = Mathf.Sqrt(distSqr);
        float strength = (AvoidRadius - dist) / AvoidRadius;
        force += away.normalized * strength;
        count++;
    }

    #endregion
}
