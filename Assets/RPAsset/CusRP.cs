using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Unity.Collections;

public class CusRP : RenderPipeline
{
    #region var
    public struct CusRPParam
    {
        public Color ClearColor;
        public bool DynamicBatcher;
        public bool SRPBatcher;
        public bool GPUInstancing;

        public bool CastShadows;
        public int ShadowAltasSize;
        public float ShadowMaxDistance;
        public float ShadowFadeFactor;
        public float CascadeFadeFactor;

        public float ShadowDepthBias;
        public float NormalBias;
        public bool UseCascade;
        public float[] CascadeRadio;
        public CusRPAsset.FilterMode FilterMode;
        public float CascadeBlendFactor;
    }
    CusRPParam m_param;
    Material m_error_mat;
    #endregion 

    #region 基本流程
    public void Setup(in CusRPParam param)
    {
        m_param = param;
    }

    #endregion

    //添加支持的pass名称
    ShaderTagId[] GenSupportShaderID()
    {
        ShaderTagId[] res =
        {
           new ShaderTagId("SRPDefaultUnlit"),
           new ShaderTagId("CusRP"),
        };
        return res;
    }
    void InitDrawSettings(ref DrawingSettings drawing_setting)
    {
        ShaderTagId[] shaders = GenSupportShaderID();
        for (int i = 0; i < shaders.Length; ++i)
        {
            drawing_setting.SetShaderPassName(i, shaders[i]);
        }
        drawing_setting.enableDynamicBatching = m_param.DynamicBatcher;
        drawing_setting.enableInstancing = m_param.GPUInstancing;
    }
    void DrawOpaque(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        DrawingSettings draw_setting = new DrawingSettings();
        draw_setting.sortingSettings = sortingSettings;
        InitDrawSettings(ref draw_setting);
        FilteringSettings filter_setting = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cull_res, ref draw_setting, ref filter_setting);
    }

    void DrawTransparent(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        DrawingSettings draw_setting = new DrawingSettings();
        draw_setting.sortingSettings = sortingSettings;
        InitDrawSettings(ref draw_setting);
        FilteringSettings filter_setting = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cull_res, ref draw_setting, ref filter_setting);
    }

    void DrawUnSupportedShader(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull_res)
    {
#if !UNITY_EDITOR
        return;
#endif

        if (null == m_error_mat)
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
            new ShaderTagId("VertexLM"),
            };
        DrawingSettings drawingSettings = new DrawingSettings(unsupported_shader_tag_ids[0], sortingSettings)
        {
            overrideMaterial = m_error_mat
        };
        for (int i = 1; i < unsupported_shader_tag_ids.Length; ++i)
        {
            drawingSettings.SetShaderPassName(i, unsupported_shader_tag_ids[i]);
        }
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
        context.DrawRenderers(cull_res, ref drawingSettings, ref filteringSettings);
    }
    void DrawGizmos(ScriptableRenderContext context, Camera cam)
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(cam, GizmoSubset.PreImageEffects);
            context.DrawGizmos(cam, GizmoSubset.PostImageEffects);
        }
    }

    void SetPreRenderSetting(ScriptableRenderContext context, CommandBuffer cmd, Camera cam)
    {
        context.SetupCameraProperties(cam);

        cmd.ClearRenderTarget(true, true, m_param.ClearColor);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    
    void RenderSingleCamera(ScriptableRenderContext context, Camera cam)
    {
        //cull
        CommandBuffer cmd = CommandBufferPool.Get("CusRP Render : " + cam.name);
        using (new ProfilingSample(cmd, "Render loop"))
        {
            ScriptableCullingParameters cull_param = new ScriptableCullingParameters();
            CullingResults cull_res;
            if(cam.TryGetCullingParameters(out cull_param))
            {
                cull_param.shadowDistance = Mathf.Min(m_param.ShadowMaxDistance, cam.farClipPlane);
                cull_res = context.Cull(ref cull_param);
            
                //设置light相关
                SetupLights(cull_res.visibleLights);
                //设置阴影相关
                SetupShadows(context, cmd, cull_res);

                SetPreRenderSetting(context, cmd, cam);
                //具体绘制
                SortingSettings sortingSettings = new SortingSettings(cam);
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
                context.Submit();
                CommandBufferPool.Release(cmd);
            }
            else
            {
                Debug.Log("Cull wrong");
            }
        }
       
    }

    #region inherit
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        QualitySettings.shadows = ShadowQuality.All;
        GraphicsSettings.useScriptableRenderPipelineBatching = m_param.SRPBatcher;
        foreach (Camera cam in cameras)
        {
            RenderSingleCamera(context, cam);
        }
    }
    #endregion

    #region light
    private LightUtil m_light_util = new LightUtil();
    void SetupLights(Unity.Collections.NativeArray<VisibleLight> lights)
    {
        m_light_util.Setup(lights);

    }
    #endregion

    #region shadow
    private ShadowUtil m_shadow_util = new ShadowUtil();
    void SetupShadows(ScriptableRenderContext context, CommandBuffer cmd, in CullingResults cull_res)
    {
        if(!m_param.CastShadows)
        {
            return;
        }
        ShadowSetting setting;
        setting.Size = m_param.ShadowAltasSize;
        setting.CascadeRadio = m_param.CascadeRadio;
        setting.UseCascade = m_param.UseCascade;
        setting.MaxDistance = m_param.ShadowMaxDistance;
        setting.ShadowFadeFactor = m_param.ShadowFadeFactor;
        setting.CascadeFadeFactor = m_param.ShadowFadeFactor;
        setting.FilterMode = m_param.FilterMode;
        setting.CascadeBlendFactor = m_param.CascadeBlendFactor;
        m_shadow_util.Setup(in setting, context, cmd, cull_res);
    }
    #endregion
}
