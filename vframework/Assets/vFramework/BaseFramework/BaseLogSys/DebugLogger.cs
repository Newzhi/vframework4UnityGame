using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 封装的日志输出器,这是一个工具类：用于支持项目其他日志输出需要
/// 1.需要输出到指定目录路径的让真机环境也能获取信息，处理报错异常或者获取关键数据来优化代码
/// 2.其他模块调用简单，类似直接Debug.Log("");输出信息
/// </summary>
public static class DebugLogger
{
    /// <summary>
    /// 根据所在平台，输出日志到指定目录
    /// </summary>
    static void Log()
    {
        
#if UNITY_EDITOR        
        //编辑器下
#endif        
        
        
    }
}
