using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using System;

public class InitSceneUtilEditorComponent
{
    [InfoBox("输入需要创建的数量、分布的范围和需要重复的实体等信息,按下按钮走之后会初始化当前场景")]
    [LabelText("数量")]
    [LabelWidth(300)]
    [MinValue(0)]
    public int Count = 10;

    [LabelText("分布的范围 x: width y:height z:depth")]
    [LabelWidth(300)]
    [MinValue(0)]
    public Vector3 Area = new Vector3(10, 10, 10);

    [TableList(ShowIndexLabels = true)]
    public List<GameObject> ObjList = new List<GameObject>();

    public bool m_use_lit = false;


    List<GameObject> m_gen_objs = new List<GameObject>();
    void ClearOldObjs()
    {
        if(m_gen_objs.Count <= 0)
        {
            return;
        }
        foreach(var obj in m_gen_objs)
        {
            GameObject.DestroyImmediate(obj);
        }
        m_gen_objs.Clear();
    }

    //材质相同
    GameObject CloneRandomSingleObj()
    {
        int obj_index = UnityEngine.Random.Range(0, ObjList.Count);
        Vector3 pos = new Vector3()
        {
            x = UnityEngine.Random.Range(-Area.x,  Area.x),
            y = UnityEngine.Random.Range(-Area.y, Area.y),
            z = UnityEngine.Random.Range(-Area.z, Area.z)
        };
        Quaternion qua = new Quaternion()
        {
            x = UnityEngine.Random.Range(0.0f, 360f),
            y = UnityEngine.Random.Range(0.0f, 360f),
            z = UnityEngine.Random.Range(0.0f, 360f),
            w = UnityEngine.Random.Range(0.0f, 360f)
        };
        return GameObject.Instantiate(ObjList[obj_index], pos, qua);
    }
    Color GenRadomColor()
    {
        Color col = Color.white;
        col.r = UnityEngine.Random.Range(0f, 1f);
        col.g = UnityEngine.Random.Range(0f, 1f);
        col.b = UnityEngine.Random.Range(0f, 1f);
        return col;
    }
    //材质不同，但是shader和mesh相同，per material属性可以不一样
    GameObject GenRandomSingleObj(Material default_mat)
    {
        int obj_index = UnityEngine.Random.Range(0, ObjList.Count);
        Vector3 pos = new Vector3()
        {
            x = UnityEngine.Random.Range(-Area.x,  Area.x),
            y = UnityEngine.Random.Range(-Area.y, Area.y),
            z = UnityEngine.Random.Range(-Area.z, Area.z)
        };
        Quaternion qua = new Quaternion()
        {
            x = UnityEngine.Random.Range(0.0f, 360f),
            y = UnityEngine.Random.Range(0.0f, 360f),
            z = UnityEngine.Random.Range(0.0f, 360f),
            w = UnityEngine.Random.Range(0.0f, 360f)
        };
        GameObject obj = GameObject.Instantiate(ObjList[obj_index], pos, qua);
        obj.transform.localScale *= UnityEngine.Random.Range(0f, 1f);
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(default_mat);
        renderer.sharedMaterial.SetColor("_Color", GenRadomColor());
        return obj;
    }

    [ButtonGroup()]
    [Button("克隆", ButtonSizes.Large)]
    public void Clone()
    {
         if(!CheakValid())
        {
            Debug.LogError("wrong input ！");
            return;
        }
        ClearOldObjs();
        for(int i = 0; i < Count; ++i)
        {
            m_gen_objs.Add(CloneRandomSingleObj());
        }
    }

    [ButtonGroup()]
    [Button("生成", ButtonSizes.Large)]
    public void Gen()
    {
        if(!CheakValid())
        {
            Debug.LogError("wrong input ！");
            return;
        }
        string mat_path = @"Assets/Materials/" + (m_use_lit ? "CusLitMat.mat" : "CusUnlitMat.mat");
        Material default_mat = AssetDatabase.LoadAssetAtPath(mat_path, typeof(Material)) as Material;
        if(null == default_mat)
        {
            Debug.LogError(" Wrong path at : " + mat_path);
            return;
        }
        ClearOldObjs();
        for(int i = 0; i < Count; ++i)
        {
            m_gen_objs.Add(GenRandomSingleObj(default_mat));
        }
    }
    [ButtonGroup()]
    [Button("摧毁", ButtonSizes.Large)]
    public void Destory()
    {
        ClearOldObjs();
    }

    
    bool CheakValid()
    {
        if(ObjList.Count <= 0)
        {
            return false;
        }
        else 
        {
            foreach(var obj in ObjList)
            {
                if(null == obj)
                {
                    return false;
                }
            }
        }
        if(Area.x <= 0 || Area.y <=0 || Area.z<=0)
        {
            return false;
        }
        if(Count <= 0)
        {
            return false;
        }
        return true;
    }
}
