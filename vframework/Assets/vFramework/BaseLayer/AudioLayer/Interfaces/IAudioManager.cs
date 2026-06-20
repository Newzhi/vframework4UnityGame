using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 使用资源管理系统来加载对应资源
/// 这个类是用来管理音频的，提供一些对外接口
/// 由IAudioPlayer来实现具体的播放等方法,这个类不负责播放，只负责管理
/// 需要解决的业务问题：
/// 1.战斗场景SFX和BGM的音乐资源可能是跨场景的，如何解决？
/// 2.一般AudioListener只能有一个，怎么解决SFX和BGM同时播放时的问题？怎么解决切换场景时候Listener的位置问题？
/// 3.是否性能良好，能够正确复用资源？
/// 4。如何控制全局的音量或者音效大小等参数？
/// </summary>
public interface IAudioManager
{
    
}
