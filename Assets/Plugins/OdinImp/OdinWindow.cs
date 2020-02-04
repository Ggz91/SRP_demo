using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

public class OdinPluginWindow : OdinMenuEditorWindow
{
    [MenuItem("CusPlugins/OdinPlugins")]
    private static void ShowWindow()
    {
        GetWindow<OdinPluginWindow>().Show();
    }
    static InitSceneUtilEditorComponent m_init_scene_component = new InitSceneUtilEditorComponent();
    static OdinMenuTree tree = new OdinMenuTree();
    protected override OdinMenuTree BuildMenuTree()
    {
        //var tree = new OdinMenuTree();
        tree.Selection.SupportsMultiSelect = false;
        tree.Add("InitScene", m_init_scene_component);
        return tree;
    }
}
