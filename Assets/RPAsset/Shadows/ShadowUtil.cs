using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public struct ShadowSetting
{
    public int Size;
    public bool UseCascade;
    public float[] CascadeRadio;
    public float MaxDistance;
    public float ShadowFadeFactor;
    public float CascadeFadeFactor;
    public CusRPAsset.FilterMode FilterMode;
    public float CascadeBlendFactor;
}

public class ShadowUtil
{
    #region var
    ShadowSetting m_setting;
    ScriptableRenderContext m_context;
    CommandBuffer m_cmd_buffer;
    CullingResults m_cull_res;

    const int m_max_lights_count = 4;
    const int m_max_cascade_count = 4;
    string m_tag = "RenderShadows";
    Matrix4x4[] m_shadow_light_space_matrics = new Matrix4x4[m_max_lights_count * m_max_cascade_count];
    //Shader 相关属性
    //Altas 的buff id
    int m_shadow_map_atlas_id;
    //灯光数量
    int m_shadow_light_count_id;
    //从世界坐标系到灯光坐标系的变换矩阵
    int m_shadow_light_space_matrics_id;
    //Cascade Cull Sphere
    int m_shadow_cascade_cull_sphere_id;
    //用来记录Cascade Cull Sphere的球心位置
    Vector4[] m_cascade_cull_sphere_split_data = new Vector4[m_max_cascade_count * m_max_lights_count];
    //shadow max distance
    int m_shadow_max_distance_id;
    //shadow fade 
    int m_shadow_fade_id;
    int m_shadow_normal_bias_id;

    float[] m_shadow_normal_bias =  new float[m_max_lights_count];
    int m_shadow_strength_id;
    float[] m_shadow_strength = new float[m_max_lights_count];
    int m_shadow_cascade_id;
    Vector4[] m_shadow_cascade_data = new Vector4[m_max_lights_count * m_max_lights_count];
    int m_shadow_altas_size_id;
    int m_shader_cascade_blend_id;
    #endregion
    
    #region method
    public void Setup(in ShadowSetting setting, ScriptableRenderContext context, CommandBuffer cmd, in CullingResults cull_res)
    {
        m_context = context;
        m_cull_res = cull_res;
        m_cmd_buffer = cmd;
        m_setting = setting;
        ExecuteImp();    
    }
    void SetShaderKeyword(string key_word, bool enable)
    {
        if(enable)
        {
            Shader.EnableKeyword(key_word);
        }
        else
        {
            Shader.DisableKeyword(key_word);
        }
    }
    void EnableFilterMode()
    {
        string[] FilterModeKeywords = 
        {
            "_",
            "_PCF3x3",
            "_PCF5x5",
            "_PCF7x7",
        };
        for(int i=0; i<=(int)CusRPAsset.FilterMode.PCF7x7; ++i)
        {
            SetShaderKeyword(FilterModeKeywords[i], i == (int)m_setting.FilterMode);
        }
    }
    void InitShadowMapAltas()
    {
        Clean();

        //分配属性id
        m_shadow_map_atlas_id = Shader.PropertyToID("_ShadowMapAltas");
        m_shadow_light_count_id = Shader.PropertyToID("_ShadowLightsCount");
        m_shadow_light_space_matrics_id = Shader.PropertyToID("_ShadowLightSpaceTransformMatrics");
        m_shadow_cascade_cull_sphere_id = Shader.PropertyToID("_ShadowCascadeCullSphereInfo");
        m_shadow_max_distance_id = Shader.PropertyToID("_ShadowMaxDistance");
        m_shadow_fade_id = Shader.PropertyToID("_ShadowFadeParam");
        m_shadow_normal_bias_id = Shader.PropertyToID("_ShadowNormalBias");
        m_shadow_strength_id = Shader.PropertyToID("_ShadowStrength");
        m_shadow_cascade_id = Shader.PropertyToID("_ShadowCascadeData");
        m_shadow_altas_size_id = Shader.PropertyToID("_ShadowAltasSize");
        m_shader_cascade_blend_id = Shader.PropertyToID("_ShadowCascadeBlend");
        //给属性赋值
        Shader.SetGlobalInt(m_shadow_light_count_id, m_cull_res.visibleLights.Length);
        Shader.SetGlobalFloat(m_shadow_max_distance_id, m_setting.MaxDistance);
        float f = 1 - m_setting.CascadeFadeFactor;
        Shader.SetGlobalVector(m_shadow_fade_id, new Vector4(1.0f/m_setting.MaxDistance, 1.0f/m_setting.ShadowFadeFactor, 1.0f / (1.0f - f * f), 0));
        Shader.SetGlobalVector(m_shadow_altas_size_id, new Vector4(m_setting.Size, 1.0f/m_setting.Size));
        Shader.SetGlobalFloat(m_shader_cascade_blend_id, m_setting.CascadeBlendFactor);
        //根据是否开启了Cascade Shadow来开启关键字
        SetShaderKeyword("_USE_CASCADE_SHADOW", m_setting.UseCascade);
        EnableFilterMode();
    }
    Vector2Int CalLightOffset(int light_index, int cascade_index, int light_count)
    {
        //不同灯光的viewport按照从左到右的顺序排列，从下到上的顺序；
        //最多4个光源
        int tile_count = light_count <= 1 ? 1 : 2;
        Vector2Int offset = new Vector2Int();
        offset.x = light_index % tile_count;
        offset.y = light_index / tile_count;
        if(m_setting.UseCascade)
        {
            //Cascade的排列顺序跟光的也一样
            int cascade_size = 2;
            offset *= cascade_size;
            offset.x += cascade_index % cascade_size;
            offset.y += cascade_index / cascade_size;
        }
        return offset;
    }
    int TileSize
    {
        get
        {
            int tile_count = m_cull_res.visibleLights.Length <= 1 ? 1 : 2;
            return m_setting.Size / tile_count;
        }
    }
    
    Rect GetShadowMapRect(int light_index, int cascade_index, int light_count, int tile_size)
    {
        Vector2 offset = CalLightOffset(light_index, cascade_index, light_count);
        return new Rect(offset.x * tile_size, offset.y * tile_size , tile_size, tile_size);
    }
    void RecordLightShadowTransMatrix(int light_count, in Vector2Int offset,Matrix4x4 view_matrix, Matrix4x4 proj_matrix)
    {
         //灯光和frag都在world space
        //要把frag转换到灯光的空间，类似从world space到view space的变换。在view space中，以camera 为原点，camera在世界空间的forward up right三个向量作为view space空间的坐标系基
        //这里类似，从world space 到light space的变换。也可以类似的看做以light的世界坐标right up forward作为light space的坐标系的基
        //因此，整个变换是基于坐标系基的变换
        //此外，还需要把clip space[-1, 1]的坐标范围转换到[0,1]。然后算上当前的tile offset
        Matrix4x4 m = proj_matrix * view_matrix;
        //判断是否Z是reserve
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        
        float scale = 1f / light_count;
        if(m_setting.UseCascade)
        {
            scale /= 2;
        }
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = (0.5f * (m.m20 + m.m30));
        m.m21 = (0.5f * (m.m21 + m.m31));
        m.m22 = (0.5f * (m.m22 + m.m32));
        m.m23 = (0.5f * (m.m23 + m.m33));
        int size = m_setting.UseCascade ? 2 : 1;
        size *= m_cull_res.visibleLights.Length > 1 ? 2 : 1;
        int index = offset.y * size + offset.x;
        m_shadow_light_space_matrics[index] = m;
    }
    void RecordLightShadowCascadeInfo(in Vector2Int offset, ref ShadowSplitData split_data)
    {
        if(!m_setting.UseCascade)
        {
            return;
        }
        
        int size = m_setting.UseCascade ? 2 : 1;
        size *= m_cull_res.visibleLights.Length > 1 ? 2 : 1;
        int index = offset.y * size + offset.x;
        
        float texel_size = 4f * split_data.cullingSphere.w / TileSize;
        float filter_size = texel_size * ((float) m_setting.FilterMode);
        //记录Cascade 相关
        m_shadow_cascade_data[index].x = 1 / split_data.cullingSphere.w;
        m_shadow_cascade_data[index].y = Mathf.Sqrt(2) * filter_size;
        //记录球心位置和半径
        m_cascade_cull_sphere_split_data[index] = split_data.cullingSphere;
        //节省一次在shader中的sqrt计算
        m_cascade_cull_sphere_split_data[index].w -= filter_size;
        m_cascade_cull_sphere_split_data[index].w *= m_cascade_cull_sphere_split_data[index].w;
        //Debug.Log("record cull sphere index : " + index.ToString());
       
    }
    void AddLightShadowParam(int light_index, int light_count, int cascade_index,Matrix4x4 view_matrix, Matrix4x4 proj_matrix, ref ShadowSplitData split_data)
    {
        Vector2Int offset = CalLightOffset(light_index, cascade_index, light_count);

        //记录矩阵
        RecordLightShadowTransMatrix(light_count, offset, view_matrix, proj_matrix);

        //记录Cascade相关
        RecordLightShadowCascadeInfo(in offset, ref split_data);

        //记录normalbias
        m_shadow_normal_bias[light_index] = m_cull_res.visibleLights[light_index].light.shadowNormalBias;
        //记录strength
        m_shadow_strength[light_index] = m_cull_res.visibleLights[light_index].light.shadowStrength;
    }
    void DrawShadowWithCascade()
    {
        int cascade_count = m_setting.CascadeRadio.Length + 1;
        Vector3 cascade_radio = new Vector3(m_setting.CascadeRadio[0], m_setting.CascadeRadio[1], m_setting.CascadeRadio[2]);
        int cascade_tile_size = TileSize / 2;
        for(int i = 0; i<m_cull_res.visibleLights.Length; ++i)
        {
            for(int j = 0; j <= m_setting.CascadeRadio.Length; ++j)
            {
                ShadowDrawingSettings settings = new ShadowDrawingSettings(m_cull_res, i);
                m_cull_res.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                i, j, cascade_count, cascade_radio, cascade_tile_size, m_cull_res.visibleLights[i].light.shadowNearPlane,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);
                settings.splitData = splitData;
                m_cmd_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                m_cmd_buffer.SetViewport(GetShadowMapRect(i, j, m_cull_res.visibleLights.Length, cascade_tile_size));
                m_cmd_buffer.SetGlobalDepthBias(0f, m_cull_res.visibleLights[i].light.shadowBias);
                ExecuteBuffer();
                m_context.DrawShadows(ref settings);
                m_cmd_buffer.SetGlobalDepthBias(0f, 0f);
                ExecuteBuffer();

                //变换矩阵存着给后续的采样做准备
                AddLightShadowParam(i, m_cull_res.visibleLights.Length, j,viewMatrix, projectionMatrix, ref splitData);
            }
        }
    }
    void DrawShadowWithoutCascade()
    {
        for(int i = 0; i<m_cull_res.visibleLights.Length; ++i)
        {
            //设置shadow渲染参数
            ShadowDrawingSettings settings = new ShadowDrawingSettings(m_cull_res, i);
            m_cull_res.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			i, 0, 1, Vector3.zero, TileSize, m_cull_res.visibleLights[i].light.shadowNearPlane,
			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData);
            settings.splitData = splitData;
            m_cmd_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            m_cmd_buffer.SetViewport(GetShadowMapRect(i, 0, m_cull_res.visibleLights.Length, TileSize));
            m_cmd_buffer.SetGlobalDepthBias(0f, m_cull_res.visibleLights[i].light.shadowBias);
            ExecuteBuffer();
            m_context.DrawShadows(ref settings);
            m_cmd_buffer.SetGlobalDepthBias(0f, 0f);
            ExecuteBuffer();
            //变换矩阵存着给后续的采样做准备
            AddLightShadowParam(i, m_cull_res.visibleLights.Length, 0,viewMatrix, projectionMatrix, ref splitData);
        }
    }
    void DrawShowsImp()
    {
       if(m_setting.UseCascade)
       {
           DrawShadowWithCascade();
       }
       else
       {
           DrawShadowWithoutCascade();
       }
    }

    void SetPreRenderDrawSetting()
    {
        //1、 分配渲染纹理
        m_cmd_buffer.GetTemporaryRT(m_shadow_map_atlas_id, m_setting.Size, m_setting.Size, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

        //2、 设置渲染目标
        m_cmd_buffer.SetRenderTarget(m_shadow_map_atlas_id, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_cmd_buffer.ClearRenderTarget(true, false, Color.clear);
        ExecuteBuffer();
    }
    void SetAfterRenderSetting()
    {
        //设置后续的变换矩阵给采样做准备
        Shader.SetGlobalMatrixArray(m_shadow_light_space_matrics_id, m_shadow_light_space_matrics);
        Shader.SetGlobalFloatArray(m_shadow_normal_bias_id, m_shadow_normal_bias);
        Shader.SetGlobalFloatArray(m_shadow_strength_id, m_shadow_strength);
        Shader.SetGlobalVectorArray(m_shadow_cascade_id, m_shadow_cascade_data);
        //设置Cascade Cull Sphere相关信息
        if(m_setting.UseCascade)
        {
            Shader.SetGlobalVectorArray(m_shadow_cascade_cull_sphere_id, m_cascade_cull_sphere_split_data);
        }
    }
    void ExecuteImp()
    {
        //1、初始化Shadow Map Altas ：大小、级联数量、灯光数量
        InitShadowMapAltas();

        //2、设置渲染前的参数
        SetPreRenderDrawSetting();

        //3、正式渲染
        DrawShowsImp();

        //4、 后处理
        SetAfterRenderSetting();
    }

    void Clean()
    {
        //清除shadow Map Altas
        m_cmd_buffer.ReleaseTemporaryRT(m_shadow_map_atlas_id);
        ExecuteBuffer();
    }

    void ExecuteBuffer()
    {
        m_context.ExecuteCommandBuffer(m_cmd_buffer);
        m_cmd_buffer.Clear();
    }
    #endregion
}
