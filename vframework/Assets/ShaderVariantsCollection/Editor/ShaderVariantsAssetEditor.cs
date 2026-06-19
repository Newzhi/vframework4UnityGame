using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering;

[CustomEditor(typeof(ShaderVariantsAsset))]
public class ShaderVariantsAssetEditor : Editor
{
    ShaderVariantsAsset asset;

    private void OnEnable()
    {
        asset = (ShaderVariantsAsset)target;
    }

    public override void OnInspectorGUI()
    {
        GUI.enabled = false;
        EditorGUILayout.TextField("TargetPath", asset.TargetPath);
        GUI.enabled = true;
        base.OnInspectorGUI();

        if (GUILayout.Button("Collect"))
        {
            //Work();
            ShaderVariantsAssetEditorStatic.Work();
        }
    }

    private List<Material> materialList;

    private void Work()
    {
        Init();

        foreach (Shader _shader in asset.shaderList)
        {
            if (_shader == null) continue;
            WorkShader(_shader);
        }

        EditorUtility.ClearProgressBar();
    }

    private void Init()
    {
        EditorUtility.DisplayProgressBar("Collecting", "Finding...", 0);
        string[] allGuids = AssetDatabase.FindAssets("t:Material", asset.resourceList);
        materialList = new List<Material>();

        for (int i = 0; i < allGuids.Length; i++)
        {
            string path = allGuids[i];
            EditorUtility.DisplayProgressBar("Collecting", path, i * 1.0f / allGuids.Length);
            
            string assetPath = AssetDatabase.GUIDToAssetPath(path);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat != null && !materialList.Contains(mat))
            {
                materialList.Add(mat);
            }
        }



        if (asset.SVCList != null)
        {
            for (int i = 0; i < asset.SVCList.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Deleting", "", i * 1.0f / asset.SVCList.Count);
                if (asset.SVCList[i] != null)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset.SVCList[i]));
                    AssetDatabase.Refresh();
                }
            }
        }

        asset.SVCList = new List<ShaderVariantCollection>();
    }

    public static bool IsShaderForSRP(Shader shader)
    {
        // 获取Shader的路径
        string shaderPath = AssetDatabase.GetAssetPath(shader);
        //Debug.LogError(shaderPath);
        // 1. 检查路径中是否包含 SRP 相关的包
        if (shaderPath.Contains("Packages/com.unity.render-pipelines"))
        {
            return true;
        }

        // 2. 读取Shader的源代码，分析是否包含SRP特有的宏或头文件
        string shaderCode = File.ReadAllText(shaderPath);
        //Debug.LogError(shaderCode);

        // 检查是否包含SRP相关的宏或头文件
        if (shaderCode.Contains("Packages/com.unity.render-pipelines") ||
            shaderCode.Contains("#pragma only_renderers") ||
            shaderCode.Contains("#include \"Packages/com.unity.render-pipelines"))
        {
            return true;
        }

        // 3. 判断是否有渲染管线的相关函数（例如：RenderPipeline、SRP特有的API等）
        if (shaderCode.Contains("RenderPipeline") ||
            shaderCode.Contains("UniversalRenderPipeline") ||
            shaderCode.Contains("HighDefinitionRenderPipeline"))
        {
            return true;
        }

        return false;
    }

    private void WorkShader(Shader _shader)
    {
        //1.我的想法是，目前先不对不合法的变体进行判断。
        //2.如今，不需要处理multicompile的变体，只需要处理shaderfeature，因此直接把材质上的keywords拿来用就好了
        //3.此外，经过测试，不需要在SVC中加入所有pass的变体集，或许用normalPass或者SRPPass就够了
        //  不处理depthonly等pass也可以得到正确的效果。
        //  只不过不确定这么搞，会不会导致meta等不需要的pass被打进去，增加内存
        //4.不合法的变体不考虑，默认是shaderGUI的问题，通过改ShaderGUI解决
        //5.其他剔除规则在IPreprocessShaders.OnProcessShader里执行

        ShaderVariantCollection svc = new ShaderVariantCollection();

        List<string> features = new List<string>();
        var targetID = _shader.FindPassTagValue(0, new ShaderTagId("RenderPipeline"));
        bool isSRP = IsShaderForSRP(_shader);
        for (int i = 0; i < materialList.Count; i++)
        {
            Material mat = materialList[i];
            if (mat.shader != _shader) continue;

            EditorUtility.DisplayProgressBar(_shader.name, mat.name, i * 1.0f / materialList.Count);

            string[] keywords = mat.shaderKeywords;

            ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant();

            sv.shader = _shader;
            if (isSRP)
                sv.passType = PassType.ScriptableRenderPipeline;
            else
                sv.passType = PassType.Normal;
            sv.keywords = keywords;

            svc.Add(sv);
        }

        if (svc.variantCount > 0)
        {
            string name = _shader.name;
            name = name.Replace(" ", "_");
            name = name.Replace("/", "_");

            AssetDatabase.CreateAsset(svc, asset.TargetPath + "/SVC_" + name + ".shadervariants");
            AssetDatabase.Refresh();

            asset.SVCList.Add(svc);
        }
    }
}
