using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 业务侧音乐播放器，可以播放音频资源，包括BGM，音效，语音等
/// 对音频的管理由AudioManager来管理
/// </summary>
public interface IAudioPlayer
{
    void PlayBGM(string clipName);
    void PlaySound(string clipName);
    void PlayVoice(string clipName);
    void StopBGM();
    void StopSound();
    void StopVoice();
    void PauseBGM();
    void PauseSound();
    void PauseVoice();
    void ResumeBGM();
}
