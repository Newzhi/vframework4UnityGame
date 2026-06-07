/*------0
此脚本是打包规则的制定者，可以让用户选择是那种平台打包，哪张方式打包（编辑器测试，联网，真机平台下的包）
0------*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BundleRuleMaker
{
    #region 设置
    private string SettingPath;
    #endregion
    
    #region Unity编辑器顶部的工具调用呼出菜单

    //将用户设置的打包参数写入xml或者SO中，其他的类根据这个规则来解依赖自动加载
    void Do()
    {
        
    }
    #endregion
}
