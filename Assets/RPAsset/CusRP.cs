using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class CusRP : RenderPipeline
{
    #region var
    public struct CusRPParam
    {
        public Color ClearColor;
    }
    CusRPParam m_param ;
    Material m_error_mat;
    #endregion 
    #region public method
    public void Setup(in CusRPParam param )
    {
        m_param = param;
    }

    #endregion
    
    void DrawOpaque(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        DrawingSettings draw_setting = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
        FilteringSettings filter_setting = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cull_res, ref draw_setting, ref filter_setting);
    }
    
    void DrawTransparent(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        DrawingSettings draw_setting = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
        FilteringSettings filter_setting = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cull_res, ref draw_setting, ref filter_setting);
    }

    void DrawUnSupportedShader(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        #if !UNITY_EDITOR
        return;
        #endif

        if(null == m_error_mat)
        {
            m_error_mat = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        ShaderTagId[] unsupported_shader_tag_ids = {
            new ShaderTagId("Always"),
		    new ShaderTagId("ForwardBase"),
		    new ShaderTagId("PrepassBase"),
		    new ShaderTagId("Vertex"),
		    new ShaderTagId("VertexLMRGBM"),
		    new ShaderTagId("VertexLM")};
        DrawingSettings drawingSettings = new DrawingSettings(unsupported_shader_tag_ids[0], sortingSettings)
        {
            overrideMaterial = m_error_mat
        };
        for(int i = 1; i<unsupported_shader_tag_ids.Length; ++i)
        {
            drawingSettings.SetShaderPassName(i, unsupported_shader_tag_ids[i]);
        }
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
        context.DrawRenderers(cull_res, ref drawingSettings, ref filteringSettings);
    }
    void DrawGizmos(ScriptableRenderContext context, Camera cam)
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(cam, GizmoSubset.PreImageEffects);
            context.DrawGizmos(cam, GizmoSubset.PostImageEffects);
        }
    }
    void RenderSingleCamera(ScriptableRenderContext context, Camera cam)
    {
        //cull
        ScriptableCullingParameters  cull_param = new ScriptableCullingParameters();
        if(cam.TryGetCullingParameters(out cull_param))
        {
            context.SetupCameraProperties(cam);
     
            CommandBuffer cmd = CommandBufferPool.Get(cam.name);
            cmd.Clear();
            using (new ProfilingSample(cmd, "Render Begin"))
            {
                cmd.ClearRenderTarget(true,true, m_param.ClearColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CullingResults cull_res = context.Cull(ref cull_param);
                SortingSettings sortingSettings = new SortingSettings(cam);
                //具体绘制
                //绘制不透明
                DrawOpaque(context, sortingSettings, cull_res);
                //绘制天空盒
                context.DrawSkybox(cam);
                //绘制透明
                DrawTransparent(context, sortingSettings, cull_res);

                //绘制不支持的shader
                DrawUnSupportedShader(context, sortingSettings, cull_res);

                //绘制gizmos
                DrawGizmos(context, cam);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();

        }
        else
        {
            Debug.Log("fail to cull , cam : " + cam.name);
        }
    }
    
    #region inherit
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera cam in cameras)
        {
            RenderSingleCamera(context, cam);
        }
    }
    #endregion
}
