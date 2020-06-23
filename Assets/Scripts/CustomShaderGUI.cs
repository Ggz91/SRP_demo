using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CustomShaderGUI : ShaderGUI
{

    void BakedEmission(MaterialEditor materialEditor)
    {
        materialEditor.LightmapEmissionProperty();
        foreach(Material mat in materialEditor.targets)
        {
            mat.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

    }
    void CopyLightMappingProperties (MaterialProperty[] properties) 
    {
		MaterialProperty mainTex = FindProperty("_MainTex", properties, false);
		MaterialProperty baseMap = FindProperty("_Tex", properties, false);
		if (mainTex != null && baseMap != null) 
        {
			mainTex.textureValue = baseMap.textureValue;
			mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
		}
		
	}

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        EditorGUI.BeginChangeCheck();
        if(EditorGUI.EndChangeCheck())
        {
            return;
        }
        BakedEmission(materialEditor);
        CopyLightMappingProperties(properties);
    }
}
