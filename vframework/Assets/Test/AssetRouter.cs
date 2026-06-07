using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AssetSource
{
    ABUNDLE,
    RESOURCES,
    NETCDN,
    EDITORRESOURCES,
}

public class AssetRouter
{
    AssetSource source;
    //根据资源的来源自动选择用哪个加载器加载资源
    void RouteAssetSource()
    {
        
        if (source == AssetSource.ABUNDLE)
        {
            
        }
        else if(source == AssetSource.RESOURCES)
        {
            
        }
        else if (source == AssetSource.NETCDN)
        {
            
        }
        else if (source == AssetSource.EDITORRESOURCES)
        {
            
        }
        else
        {
            Debug.LogError("Invalid asset source: " + source);
        }
    }
    
}
