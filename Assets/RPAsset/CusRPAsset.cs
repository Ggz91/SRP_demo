using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

[CreateAssetMenu]
public class CusRPAsset : RenderPipelineAsset
{
    #region public var
    [System.Serializable]
    public struct SGeneralSettings
    {
        [SerializeField]
        public Color ClearColor;
    };

    [System.Serializable]
    public struct SBatcherSettings
    {
        [SerializeField]
        public bool DynamicBatcher;
        [SerializeField]
        public bool SRPBatcher;
        [SerializeField]
        public bool GPUInstancing;
    };
    
    [System.Serializable]
    public struct SShadowSettings
    {
        [SerializeField]
        public bool CastShadows;
        [SerializeField]
        public int ShadowAtlasSize;
        [Min(0.001f)]
        [SerializeField]
        public float ShadowMaxDistance;

        [SerializeField]
        public bool UseCaseCasde;
        [SerializeField]
        const int CascadeCount = 4;

        [SerializeField]
        public float[] CascadeRadio ;
        [Min(0.0001f)]
        [SerializeField]
        public float ShadowFadeFactor;
        [Min(0.0001f)]
        [SerializeField]
        public float CascadeFadeFactor;
        [SerializeField]
        public FilterMode ShadowFilterMode;
        [Range(0f, 1f)]
        [SerializeField]
        public float CascadeBlendFactor;
        [SerializeField]
        public CascadeBlendMode ShadowCascadeBlendMode;
    }
    
    public SGeneralSettings  GeneralSettings;
    public SBatcherSettings BatcherSettings;
    
    public SShadowSettings ShadowSettings;
        
    public enum FilterMode
    {
        NONE = 0,
        PCF3x3,
        PCF5x5,
        PCF7x7,
    }
    public enum CascadeBlendMode
    {
        NONE = 0,
        Dither,
    }
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
        param.ClearColor = GeneralSettings.ClearColor;
        param.DynamicBatcher = BatcherSettings.DynamicBatcher;
        param.SRPBatcher = BatcherSettings.SRPBatcher;
        param.GPUInstancing = BatcherSettings.GPUInstancing;
        param.CastShadows = ShadowSettings.CastShadows;
        param.ShadowAltasSize = ShadowSettings.ShadowAtlasSize;
        param.ShadowMaxDistance = ShadowSettings.ShadowMaxDistance;
        param.CascadeRadio = ShadowSettings.CascadeRadio;
        param.UseCascade = ShadowSettings.UseCaseCasde;
        param.ShadowFadeFactor = ShadowSettings.ShadowFadeFactor;
        param.CascadeFadeFactor = ShadowSettings.CascadeFadeFactor;
        param.FilterMode = ShadowSettings.ShadowFilterMode;
        param.CascadeBlendFactor = ShadowSettings.CascadeBlendFactor;
        param.CascadeBlendMode = ShadowSettings.ShadowCascadeBlendMode;
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
    void InitGeneralSettings()
    {
        GeneralSettings.ClearColor = new Color(1, 1, 1, 1);
    }
    void InitBatcherSettings()
    {
        BatcherSettings.DynamicBatcher = false;
        BatcherSettings.SRPBatcher = true;
        BatcherSettings.GPUInstancing = true;
    }
    void InitShadowSettings()
    {
        ShadowSettings.CastShadows = true;
        ShadowSettings.ShadowAtlasSize = 4096;
        ShadowSettings.ShadowMaxDistance = 20;
        ShadowSettings.UseCaseCasde = true;
        ShadowSettings.CascadeRadio= new float[] {0.2f, 0.3f, 0.4f};
        ShadowSettings.ShadowFadeFactor = 0.1f;
        ShadowSettings.CascadeFadeFactor = 0.1f;
        ShadowSettings.ShadowFilterMode = FilterMode.NONE;
        ShadowSettings.CascadeBlendFactor = 0.7f;
        ShadowSettings.ShadowCascadeBlendMode = CascadeBlendMode.NONE;
    }
    void InitDefaultSettings()
    {
        InitGeneralSettings();
        InitBatcherSettings();
        InitShadowSettings();

    }
    #endregion

    #region inherited
    protected override RenderPipeline CreatePipeline()
    {
        InitDefaultSettings();
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
