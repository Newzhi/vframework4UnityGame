using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IABundleManager
{
    //初始化
    public void Initialize();
    
    //同步加载资源
    public void Load<T>();
    
    //异步加载资源
    public void LoadAscyn();
    
    //完成带回调
    public void LoadWithCallBack();
    
    //卸载资源
    public void Unload();
}
