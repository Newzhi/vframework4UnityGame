using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 业务侧音乐播放器，支持播放
/// </summary>
public interface IAudioPlayer
{
    void Play(AudioClip clip);
    void Stop();
}
