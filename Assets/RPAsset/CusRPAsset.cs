using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

[ExecuteInEditMode]
public class CusRPAsset : RenderPipelineAsset
{
    #region public var
    public Color ClearColor;
    #endregion
    
    #region  private var
    static string m_asset_path = @"Assets/RPAsset/CusRPAsset.asset";

    //传递给CusRP的设置参数
    
    #endregion

    #region method
    [MenuItem("CustomRP/Create A New Custom RenderPipeline")]
    static void CreateCusRPAsset()
    {
        RenderPipelineAsset rp_asset = new CusRPAsset();
        AssetDatabase.CreateAsset(rp_asset, m_asset_path);
    }
    
    void FillRPParam(ref CusRP.CusRPParam param)
    {
        param.ClearColor = ClearColor;
    }
    #endregion

    #region inherited
    protected override RenderPipeline CreatePipeline()
    {
        RenderPipeline rp = new CusRP();
        CusRP.CusRPParam param = new CusRP.CusRPParam();
        FillRPParam(ref param);
        (rp as CusRP).Setup(param);
        return rp;
    }
    #endregion
}
