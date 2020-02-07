using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public struct ShadowSetting
{
    public int Size;
}

public class ShadowUtil
{
    #region var
    ShadowSetting m_setting;
    ScriptableRenderContext m_context;
    CommandBuffer m_cmd_buffer;
    CullingResults m_cull_res;

    const int m_max_lights_count = 4;
    string m_tag = "RenderShadows";
    List<Matrix4x4> m_shadow_light_space_matrics = new List<Matrix4x4>();
    //Shader 相关属性
    //Altas 的buff id
    int m_shadow_map_atlas_id;
    //灯光数量
    int m_shadow_light_count_id;
    //从世界坐标系到灯光坐标系的变换矩阵
    int m_shadow_light_space_matrics_id;
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

    void InitShadowMapAltas()
    {
        Clean();

        //分配属性id
        m_shadow_map_atlas_id = Shader.PropertyToID("_ShadowMapAltas");
        m_shadow_light_count_id = Shader.PropertyToID("_ShadowLightsCount");
        m_shadow_light_space_matrics_id = Shader.PropertyToID("_ShadowLightSpaceTransformMatrics");

        //给属性赋值
        Shader.SetGlobalInt(m_shadow_light_count_id, m_cull_res.visibleLights.Length);
    }
    Vector2 CalLightOffset(int light_index, int light_count)
    {
        //不同灯光的viewport按照从左到右的顺序排列，从下到上的顺序；
        //最多4个光源
        int tile_count = light_count <= 1 ? 1 : 2;
        Vector2 offset = new Vector2();
        offset.x = light_index % tile_count;
        offset.y = light_index / tile_count;
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
    Rect GetShadowMapRect(int light_index, int light_count)
    {
        Vector2 offset = CalLightOffset(light_index, light_count);
        return new Rect(offset.x * TileSize, offset.y * TileSize , TileSize, TileSize);
    }
    void AddLightSpaceTransMatrix(int i, int light_count,Matrix4x4 view_matrix, Matrix4x4 proj_matrix)
    {
         //灯光和frag都在world space
        //要把frag转换到灯光的空间，类似从world space到view space的变换。在view space中，以camera 为原点，camera在世界空间的forward up right三个向量作为view space空间的坐标系基
        //这里类似，从world space 到light space的变换。也可以类似的看做以light的世界坐标right up forward作为light space的坐标系的基
        //因此，整个变换是基于坐标系基的变换
        //此外，还需要把clip space[-1, 1]的坐标范围转换到[0,1]。然后算上当前的tile offset
        Vector2 offset = CalLightOffset(i, light_count);
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
        m_shadow_light_space_matrics.Add(m);
    }
    void DrawShowsImp()
    {
        for(int i = 0; i<m_cull_res.visibleLights.Length; ++i)
        {
            //设置shadow渲染参数
            ShadowDrawingSettings settings = new ShadowDrawingSettings(m_cull_res, i);
            m_cull_res.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			i, 0, 1, Vector3.zero, TileSize, 0f,
			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData);
            settings.splitData = splitData;
            m_cmd_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            m_cmd_buffer.SetViewport(GetShadowMapRect(i, m_cull_res.visibleLights.Length));
            m_cmd_buffer.SetGlobalDepthBias(0f, 1f);
            ExecuteBuffer();
            m_context.DrawShadows(ref settings);
            m_cmd_buffer.SetGlobalDepthBias(0f, 0f);
            ExecuteBuffer();
            //变换矩阵存着给后续的采样做准备
            AddLightSpaceTransMatrix(i, m_cull_res.visibleLights.Length,viewMatrix, projectionMatrix);
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

        m_shadow_light_space_matrics.Clear();
    }

    void ExecuteBuffer()
    {
        m_context.ExecuteCommandBuffer(m_cmd_buffer);
        m_cmd_buffer.Clear();
    }
    #endregion
}
