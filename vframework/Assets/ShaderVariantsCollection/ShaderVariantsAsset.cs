using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

[CreateAssetMenu(menuName = "Shader/Create Shader Variants Asset")]
public class ShaderVariantsAsset : ScriptableObject
{
    public string TargetPath = "Assets/Res/Shaderlib";

    public List<Shader> shaderList = null;

    public string[] resourceList = new string[]
    {
        "Assets/GeneratedRes",
        "Assets/Res/Character",
        "Assets/Res/Effect",
        "Assets/Res/Scene",
    };

    [SerializeField]
    private List<ShaderVariantCollection> svcList = null;
    public List<ShaderVariantCollection> SVCList
    {
        get
        {
            return svcList;
        }
        set
        {
            svcList = value;
        }
    }
}
