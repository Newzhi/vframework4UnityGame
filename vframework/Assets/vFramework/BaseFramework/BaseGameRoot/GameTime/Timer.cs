using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计时器，用于提供给其他模块使用的延时执行某个方法，间隔多少秒执行某个方法的功能入口
/// 使用方法：
/// 1. 创建一个Timer对象
/// 2. 调用Start方法，传入要执行的方法和间隔时间
/// 3. 调用Stop方法，停止计时
/// 4. 调用Reset方法，重置计时
/// 5. 调用Pause方法，暂停计时
/// 6. 调用Resume方法，继续计时
/// 7. 调用Toggle方法，暂停或继续计时
/// 8. 调用IsRunning方法，判断计时器是否正在运行
/// 9. 调用IsPaused方法，判断计时器是否正在暂停
/// </summary>
public class Timer 
{
    #region 字段定义
    private Action action;
    private float interval;
    private float startTime;
    private bool isRunning;
    private bool isUnscaled;
    #endregion

    #region 方法定义    
    public void Start(Action action, float interval, bool isUnscaled = false)
    {
        this.action = action;
        this.interval = interval;
        this.startTime = Time.time;
        this.isRunning = true;
        this.isUnscaled = isUnscaled;
    }               

    public void Stop()
    {
        this.isRunning = false;
    }

    public void Reset()
    {
        this.startTime = Time.time;
    }

    public void Pause()
    {
        this.isRunning = false;
    }

    public void Resume()
    {
        this.isRunning = true;
    }

    public void Toggle()
    {
        this.isRunning = !this.isRunning;
    }

    public bool IsRunning()
    {
        return this.isRunning;
    }

    public bool IsPaused()
    {
        return !this.isRunning;
    }
    #endregion
}
