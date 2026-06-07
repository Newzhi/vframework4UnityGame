using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class BundleLoader
{
    #region 初始化

    void Init(){}

    #endregion
    
    #region 加载/卸载
    
    T Load<T>()
    {
        //return AbstractResource.Load<T>;
        return default(T);
    }

    T LoadAsync<T>()
    {
        T t = default;
        return t;
    }

    void LoadWithCallback<T>()
    {
        
    }

    void Unload()
    {
        
    }

    void UnloadAll()
    {
        
    }

    #endregion
    
    #region 辅助函数
    
    #endregion

}
