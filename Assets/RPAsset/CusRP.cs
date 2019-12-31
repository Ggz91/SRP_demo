using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CusRP : RenderPipeline
{
    #region var
    public struct CusRPParam
    {
        public Color ClearColor;
    }
    CusRPParam m_param ;
    #endregion 
    #region public method
    public void Setup(in CusRPParam param )
    {
        m_param = param;
    }

    #endregion
    
    void DrawOpaque(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        DrawingSettings draw_setting = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
        FilteringSettings filter_setting = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cull_res, ref draw_setting, ref filter_setting);
    }
    
    void DrawTransparent(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        DrawingSettings draw_setting = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
        FilteringSettings filter_setting = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cull_res, ref draw_setting, ref filter_setting);
    }
    void RenderSingleCamera(ScriptableRenderContext context, Camera cam)
    {
        //cull
        ScriptableCullingParameters  cull_param = new ScriptableCullingParameters();
        if(cam.TryGetCullingParameters(out cull_param))
        {
            context.SetupCameraProperties(cam);
     
            CommandBuffer cmd = CommandBufferPool.Get(cam.name);
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
