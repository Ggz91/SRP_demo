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
    public float ShadowMaxDistance = 1000;
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
        param.ClearColor = ClearColor;
        param.DynamicBatcher = DynamicBatcher;
        param.SRPBatcher = SRPBatcher;
        param.GPUInstancing = GPUInstancing;
        param.CastShadows = CastShadows;
        param.ShadowAltasSize = ShadowAtlasSize;
        param.ShadowMaxDistance = ShadowMaxDistance;
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
