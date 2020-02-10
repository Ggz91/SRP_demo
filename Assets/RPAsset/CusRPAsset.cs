using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

[CreateAssetMenu]
public class CusRPAsset : RenderPipelineAsset
{
    #region public var
    public Color ClearColor;
    public bool DynamicBatcher;
    public bool SRPBatcher;
    public bool GPUInstancing;
    
    public bool CastShadows;
    public int ShadowAtlasSize = 512;
    [Min(0.001f)]
    public float ShadowMaxDistance = 20;
    public float ShadowDepthBias = 1f;

    public bool UsCaseCasde = false;
    const int CascadeCount = 4;

    public float[] CascadeRadio = {0.2f, 0.3f, 0.4f};
    #endregion
    
    #region  private var
    //static string m_asset_path = @"Assets/RPAsset/CusRPAsset.asset";

    RenderPipeline rp = new CusRP();
    #endregion

    #region method
    /*[MenuItem("CustomRP/Create A New Custom RenderPipeline")]
    static void CreateCusRPAsset()
    {
        RenderPipelineAsset rp_asset = new CusRPAsset();
        AssetDatabase.CreateAsset(rp_asset, m_asset_path);
    }*/
    
    void FillRPParam(ref CusRP.CusRPParam param)
    {
        CheckParam();
        param.ClearColor = ClearColor;
        param.DynamicBatcher = DynamicBatcher;
        param.SRPBatcher = SRPBatcher;
        param.GPUInstancing = GPUInstancing;
        param.CastShadows = CastShadows;
        param.ShadowAltasSize = ShadowAtlasSize;
        param.ShadowMaxDistance = ShadowMaxDistance;
        param.ShadowDepthBias = ShadowDepthBias;
        param.CascadeRadio = CascadeRadio;
        param.UseCascade = UsCaseCasde;
        param.CascadeRadio = CascadeRadio;
    }
    void CheckParam()
    {
        string res = "";
        //待检查的参数值        
        if("" != res)
        {
            Debug.LogError(res + " is wrong input ");
        }
    }
    #endregion

    #region inherited
    protected override RenderPipeline CreatePipeline()
    {
        if(null == rp || rp.disposed)
        {
            rp = new CusRP();
        }
        CusRP.CusRPParam param = new CusRP.CusRPParam();
        FillRPParam(ref param);
        (rp as CusRP).Setup(param);
        return rp;
    }
    
    protected override void OnValidate()
    {
        if(null == rp || rp.disposed)
        {
            rp = new CusRP();
        }
        CusRP.CusRPParam param = new CusRP.CusRPParam();
        FillRPParam(ref param);
        (rp as CusRP).Setup(param);
    }
    #endregion
}
