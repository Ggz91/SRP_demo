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
    int m_shadow_map_atlas_id;
    ScriptableRenderContext m_context;
    CommandBuffer m_cmd_buffer;
    CullingResults m_cull_res;

    string m_tag = "RenderShadows";
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
        m_shadow_map_atlas_id = Shader.PropertyToID("_ShadowMapAltas");
    }
    void DrawShowsImp()
    {
        for(int i = 0; i<m_cull_res.visibleLights.Length; ++i)
        {
             //设置shadow渲染参数
            ShadowDrawingSettings settings = new ShadowDrawingSettings(m_cull_res, i);
            m_cull_res.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			i, 0, 1, Vector3.zero, m_cull_res.visibleLights.Length, 0f,
			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData);
            settings.splitData = splitData;
            m_cmd_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            m_context.DrawShadows(ref settings);
        }
    }

    void SetPreRenderDrawSetting()
    {
        //1、 分配渲染纹理
        m_cmd_buffer.GetTemporaryRT(m_shadow_map_atlas_id, m_setting.Size, m_setting.Size, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

        //2、 设置渲染目标
        m_cmd_buffer.SetRenderTarget(m_shadow_map_atlas_id, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_cmd_buffer.ClearRenderTarget(true, true, Color.clear);
        ExecuteBuffer();
    }
    void SetAfterRenderSetting()
    {
        
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
