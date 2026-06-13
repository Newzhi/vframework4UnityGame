using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//规范
public interface IPool
{
    /// <summary>
    /// 初始化一个池，使用句柄来初始化，创建的时候相当于移交句柄引用计数的控制权，当一个对象池被销毁的时候，句柄也销毁
    /// 需要明确池子什么时候可以被销毁（没有对象了）
    /// </summary>
    void CreatPool();
    
    /// <summary>
    /// 从池子里获取一个资源，如果没有则生成一个
    /// </summary>
    void GetObj();
    
    /// <summary>
    /// 将获取的资源放回池子
    /// </summary>
    void ReleaseObj();
    
    /// <summary>
    /// 销毁池子和池子生成管理的实例化资源，并回收句柄
    /// </summary>
    void DestroyPool();
}
