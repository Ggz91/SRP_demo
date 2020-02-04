using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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

    GameObject GenRadomSingleObj()
    {
        int obj_index = Random.Range(0, ObjList.Count);
        Vector3 pos = new Vector3()
        {
            x = Random.Range(-Area.x,  Area.x),
            y = Random.Range(-Area.y, Area.y),
            z = Random.Range(-Area.z, Area.z)
        };
        Quaternion qua = new Quaternion()
        {
            x = Random.Range(0.0f, 360f),
            y = Random.Range(0.0f, 360f),
            z = Random.Range(0.0f, 360f),
            w = Random.Range(0.0f, 360f)
        };
        return GameObject.Instantiate(ObjList[obj_index], pos, qua);
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
        ClearOldObjs();
        for(int i = 0; i < Count; ++i)
        {
            m_gen_objs.Add(GenRadomSingleObj());
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
