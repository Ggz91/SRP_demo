using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightUtil
{
    #region var
    const int m_max_light_count = 4;
    int m_lights_dirs_id = 0;
    int m_lights_colors_id = 0;
    List<Vector4> m_list_dirs ;
    List<Vector4> m_list_colors ;
    public struct LightSetUpRes
    {
        public bool UseShadowMask;
    }   
    #endregion
    public LightUtil()
    {
        m_lights_dirs_id = Shader.PropertyToID("_LightsDirections");
        m_lights_colors_id = Shader.PropertyToID("_LightsColors");
        
        m_list_colors = new List<Vector4>(new Vector4[m_max_light_count]);
        m_list_dirs = new List<Vector4>(new Vector4[m_max_light_count]);
    }
    public void Setup(Unity.Collections.NativeArray<VisibleLight> lights, out LightSetUpRes res)
    {
        res.UseShadowMask = false;
        if(lights.Length<=0)
        {
            return;
        }
        Shader.SetGlobalInt("_LightsCount", lights.Length);
        for(int i=0; i<lights.Length && i<m_max_light_count; ++i)
        {
            VisibleLight light = lights[i];
            //暂时只针对平行光
            if(LightType.Directional != light.lightType)
            {
                continue;
            }
            if(light.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed
            && light.light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                res.UseShadowMask = true;
            }
            m_list_colors[i] = light.finalColor;
            m_list_dirs[i] = light.light.transform.forward;
        }
        Shader.SetGlobalVectorArray(m_lights_colors_id, m_list_colors);
        Shader.SetGlobalVectorArray(m_lights_dirs_id, m_list_dirs);
    }

}
