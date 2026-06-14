using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 使用资源管理系统来实现对应的播放音乐关闭音乐等功能，对游戏全局bgm,音效等有调度控制权
/// </summary>
public interface IAudioManager
{
    void PlaySound(string soundName);
    void StopSound(string soundName);
}
