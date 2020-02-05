using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BatchSetColorProps : MonoBehaviour
{
    Color GenRadomColor()
    {
        Color col = Color.white;
        col.r = Random.Range(0f, 1f);
        col.g = Random.Range(0f, 1f);
        col.b = Random.Range(0f, 1f);
        return col;
    }
    void SetPropertyBlock()
    {
        return;
        MaterialPropertyBlock prop_block = new MaterialPropertyBlock();
        //场景中所有带mesh renderer的物体
        foreach(GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if(null == renderer)
            {
                continue;
            }
            
            prop_block.SetColor("_Col", GenRadomColor());
            //设置位置
            
            renderer.SetPropertyBlock(prop_block);
        }
    }
    void SetRawColor()
    {
        Material default_mat = AssetDatabase.LoadAssetAtPath("Assets/Materials/CusLitMat.mat", typeof(Material)) as Material;
        foreach(GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if(null == renderer)
            {
                continue;
            }
            Shader shader = AssetDatabase.LoadAssetAtPath("Assets/Shaders/CusLitShader.shader", typeof(Shader)) as Shader;
            renderer.sharedMaterial = new Material(shader);
            renderer.sharedMaterial.SetColor("_Col", GenRadomColor());
            renderer.sharedMaterial.SetFloat("_SrcBlend", default_mat.GetFloat("_SrcBlend"));
            renderer.sharedMaterial.SetFloat("_DstBlend", default_mat.GetFloat("_DstBlend"));
            renderer.sharedMaterial.SetTexture("_Tex", default_mat.GetTexture("_Tex"));
            renderer.sharedMaterial.SetFloat("_ZWrite", default_mat.GetFloat("_ZWrite"));
            renderer.sharedMaterial.SetFloat("_Clip", default_mat.GetFloat("_Clip"));
            if(default_mat.IsKeywordEnabled("_CLIPPING"))
            {
                renderer.sharedMaterial.EnableKeyword("_CLIPPING");
            }
            else
            {
                renderer.sharedMaterial.DisableKeyword("_CLIPPING");
            }
        }
    }
    // Start is called before the first frame update
    void Awake()
    {
        //如果是SRP Batcher就直接设置Color，如果是GPU Instancing 就用PropertyBlock
        //根据管线的序列化属性
        Object rp_obj = AssetDatabase.LoadAssetAtPath(@"Assets/RPAsset/CusRP.asset", typeof(CusRPAsset));
        SerializedObject rp_se_obj = new SerializedObject(rp_obj);
        SerializedProperty pro = rp_se_obj.FindProperty("GPUInstancing");
        bool is_gpu_instancing = pro.boolValue;
        if(is_gpu_instancing)
        {
            SetPropertyBlock();
        }
        else
        {
           SetRawColor();
        }
    }

}
