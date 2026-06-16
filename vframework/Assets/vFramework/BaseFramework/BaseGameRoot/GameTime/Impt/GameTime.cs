using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏时间定义，数据层
/// </summary>
public class GameTime
{
    public float TimeScale { get; set; } = 1f;
    public float Time { get; set; } = 0f;
    public float DeltaTime { get; set; } = 0f;
    public float FixedDeltaTime { get; set; } = 0.02f;
    public float TimeSinceLevelLoad { get; set; } = 0f;
    public float TimeSinceStartup { get; set; } = 0f;
}
