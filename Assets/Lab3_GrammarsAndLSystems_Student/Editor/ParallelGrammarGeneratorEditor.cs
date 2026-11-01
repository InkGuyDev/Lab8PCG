using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector personalizado para ParallelGrammarGenerator.
///
/// Este script corresponde únicamente a infraestructura.
/// NO implementa la reescritura de la gramática.
/// </summary>
[CustomEditor(typeof(ParallelGrammarGenerator))]
public class ParallelGrammarGeneratorEditor : Editor
{
    private SerializedProperty autoUpdate;
    private SerializedProperty axiom;
    private SerializedProperty rules;
    private SerializedProperty iterations;
    private SerializedProperty showDerivation;

    private bool regenerationScheduled = false;


    // =====================================================================
    // ENABLE
    // =====================================================================

    private void OnEnable()
    {
        autoUpdate =
            serializedObject.FindProperty(
                "autoUpdate"
            );


        axiom =
            serializedObject.FindProperty(
                "axiom"
            );


        rules =
            serializedObject.FindProperty(
                "rules"
            );


        iterations =
            serializedObject.FindProperty(
                "iterations"
            );


        showDerivation =
            serializedObject.FindProperty(
                "showDerivation"
            );
    }


    // =====================================================================
    // INSPECTOR
    // =====================================================================

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        EditorGUI.BeginChangeCheck();


        // =============================================================
        // PARALLEL GRAMMAR
        // =============================================================

        EditorGUILayout.LabelField(
            "Parallel Grammar",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "La gramática se expande aplicando las reglas en paralelo.\n\n" +

            "Configuración base:\n\n" +

            "Axiom: A\n\n" +

            "A -> AB\n" +
            "B -> A\n\n" +

            "Con 4 iteraciones:\n\n" +

            "A\n" +
            "AB\n" +
            "ABA\n" +
            "ABAAB\n" +
            "ABAABABA",

            MessageType.Info
        );


        EditorGUILayout.Space(5);


        EditorGUILayout.PropertyField(
            autoUpdate,
            new GUIContent(
                "Auto Update",
                "Ejecuta nuevamente la expansión cuando se modifica " +
                "un parámetro."
            )
        );


        EditorGUILayout.PropertyField(
            axiom,
            new GUIContent(
                "Axiom",
                "Cadena inicial de la gramática."
            )
        );


        EditorGUILayout.PropertyField(
            rules,
            new GUIContent(
                "Rules",
                "Reglas de producción."
            ),
            true
        );


        EditorGUILayout.PropertyField(
            iterations,
            new GUIContent(
                "Iterations",
                "Cantidad de iteraciones de expansión."
            )
        );


        EditorGUILayout.PropertyField(
            showDerivation,
            new GUIContent(
                "Show Derivation",
                "Muestra el proceso completo en Console."
            )
        );


        bool changed =
            EditorGUI.EndChangeCheck();


        serializedObject.ApplyModifiedProperties();


        EditorGUILayout.Space(10);


        // =============================================================
        // CONCEPT HELP
        // =============================================================

        EditorGUILayout.HelpBox(
            "Importante:\n\n" +

            "En una iteración todos los símbolos de la cadena actual " +
            "se evalúan antes de obtener la siguiente cadena.\n\n" +

            "Si un símbolo no posee una regla asociada, debe mantenerse " +
            "sin modificaciones.",

            MessageType.None
        );


        EditorGUILayout.Space(10);


        // =============================================================
        // BUTTON
        // =============================================================

        ParallelGrammarGenerator generator =
            (ParallelGrammarGenerator)target;


        if (GUILayout.Button(
            "Generate Expansion",
            GUILayout.Height(35)))
        {
            generator.GenerateExpansion();

            EditorUtility.SetDirty(
                generator
            );
        }


        // =============================================================
        // AUTO UPDATE
        // =============================================================

        if (changed &&
            autoUpdate.boolValue)
        {
            ScheduleRegeneration(
                generator
            );
        }
    }


    // =====================================================================
    // AUTO UPDATE
    // =====================================================================

    private void ScheduleRegeneration(
        ParallelGrammarGenerator generator)
    {
        if (regenerationScheduled)
        {
            return;
        }


        regenerationScheduled =
            true;


        EditorApplication.delayCall +=
            () =>
            {
                regenerationScheduled =
                    false;


                if (generator == null)
                {
                    return;
                }


                generator.GenerateExpansion();


                EditorUtility.SetDirty(
                    generator
                );
            };
    }
}