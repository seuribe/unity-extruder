using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

//[CustomEditor(typeof(GameObjectHierarchyPath))]
public class GameObjectHierarchyPathEditor : Editor
{
    private GameObjectHierarchyPath gohp;

    public void OnEnable()
    {
        gohp = (GameObjectHierarchyPath)target;
    }

    public override void OnInspectorGUI()
    {
        gohp.PathRoot = (GameObject)EditorGUILayout.ObjectField("Path Root", gohp.PathRoot, typeof(GameObject), true);

        SceneView.RepaintAll();
    }
}
