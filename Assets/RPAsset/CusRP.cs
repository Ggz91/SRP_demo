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
    
    #region inherit
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //base.Render(context, cameras);
        CommandBuffer cmd = new CommandBuffer();
        cmd.Clear();
        cmd.ClearRenderTarget(true, true, m_param.ClearColor);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
        cmd.Release();
    }
    #endregion
}
