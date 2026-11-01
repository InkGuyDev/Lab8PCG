using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector personalizado para MissionGenerator.
///
/// Este script solamente facilita la ejecución de la generación
/// directamente desde el Inspector.
/// </summary>
[CustomEditor(typeof(MissionGenerator))]
public class MissionGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "La Console mostrará el proceso completo:\n\n" +

            "1. Reglas de la gramática\n" +
            "2. Derivación paso a paso\n" +
            "3. Cadena final\n" +
            "4. Misión interpretada\n\n" +

            "Cambiar Seed permite seleccionar una secuencia diferente " +
            "de reglas manteniendo los demás parámetros.",

            MessageType.Info
        );

        EditorGUILayout.Space(10);

        MissionGenerator generator =
            (MissionGenerator)target;

        if (GUILayout.Button(
            "Generate Mission",
            GUILayout.Height(35)))
        {
            generator.GenerateMission();
        }
    }
}