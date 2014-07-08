using UnityEngine;
using UnityEditor;

using System.Collections;

[CustomEditor(typeof(Extruder))]
public class ExtruderEditor : Editor
{
    Extruder extruder;

    public void OnEnable()
    {
        extruder = (Extruder)target;
    }

    public override void OnInspectorGUI()
    {
        extruder.SvgFile = (TextAsset)EditorGUILayout.ObjectField("SVG File", extruder.SvgFile, typeof(TextAsset), false);

//        extruder.Extrude = (Transform)EditorGUILayout.ObjectField("Extrude Transform", extruder.Extrude, typeof(Transform), true);

        extruder.material = (Material)EditorGUILayout.ObjectField("Material", extruder.material, typeof(Material), false);

        extruder.invertTop = EditorGUILayout.Toggle("Invert Top", extruder.invertTop);
        extruder.invertBottom = EditorGUILayout.Toggle("Invert Bottom", extruder.invertBottom);
        extruder.invertSides = EditorGUILayout.Toggle("Invert Sides", extruder.invertSides);
        extruder.generateCollider = EditorGUILayout.Toggle("Generate Collider", extruder.generateCollider);
        extruder.generateOnEditor = EditorGUILayout.Toggle("Generate Mesh in Editor", extruder.generateOnEditor);

        SerializedProperty tps = serializedObject.FindProperty("extrudePoints");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(tps, true);
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();

        SceneView.RepaintAll();
    }
}
