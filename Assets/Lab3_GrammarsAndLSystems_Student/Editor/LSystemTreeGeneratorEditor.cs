using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector personalizado para LSystemTreeGenerator.
///
/// Permite:
///
/// - Seleccionar modo 2D o 3D.
/// - Mostrar solamente la configuración del modo activo.
/// - Regenerar automáticamente el árbol.
/// - Configurar la visualización.
/// - Generar manualmente cuando Auto Update está desactivado.
///
/// Este script corresponde a infraestructura del laboratorio.
/// </summary>
[CustomEditor(typeof(LSystemTreeGenerator))]
public class LSystemTreeGeneratorEditor : Editor
{
    // =====================================================================
    // GENERATION
    // =====================================================================

    private SerializedProperty generationMode;
    private SerializedProperty autoUpdate;


    // =====================================================================
    // 2D
    // =====================================================================

    private SerializedProperty axiom2D;
    private SerializedProperty rules2D;
    private SerializedProperty iterations2D;
    private SerializedProperty angle2D;


    // =====================================================================
    // 3D
    // =====================================================================

    private SerializedProperty axiom3D;
    private SerializedProperty rules3D;
    private SerializedProperty iterations3D;
    private SerializedProperty angle3D;


    // =====================================================================
    // VISUALIZATION
    // =====================================================================

    private SerializedProperty segmentLength;
    private SerializedProperty branchRadius;
    private SerializedProperty branchMaterial;


    // =====================================================================
    // HEIGHT COLOR
    // =====================================================================

    private SerializedProperty useHeightColor;
    private SerializedProperty lowColor;
    private SerializedProperty highColor;
    private SerializedProperty colorHeight;


    // =====================================================================
    // DEBUG
    // =====================================================================

    private SerializedProperty showDerivation;


    // =====================================================================
    // ENABLE
    // =====================================================================

    private void OnEnable()
    {
        // -------------------------------------------------------------
        // GENERATION
        // -------------------------------------------------------------

        generationMode =
            serializedObject.FindProperty(
                "generationMode"
            );


        autoUpdate =
            serializedObject.FindProperty(
                "autoUpdate"
            );


        // -------------------------------------------------------------
        // 2D
        // -------------------------------------------------------------

        axiom2D =
            serializedObject.FindProperty(
                "axiom2D"
            );


        rules2D =
            serializedObject.FindProperty(
                "rules2D"
            );


        iterations2D =
            serializedObject.FindProperty(
                "iterations2D"
            );


        angle2D =
            serializedObject.FindProperty(
                "angle2D"
            );


        // -------------------------------------------------------------
        // 3D
        // -------------------------------------------------------------

        axiom3D =
            serializedObject.FindProperty(
                "axiom3D"
            );


        rules3D =
            serializedObject.FindProperty(
                "rules3D"
            );


        iterations3D =
            serializedObject.FindProperty(
                "iterations3D"
            );


        angle3D =
            serializedObject.FindProperty(
                "angle3D"
            );


        // -------------------------------------------------------------
        // VISUALIZATION
        // -------------------------------------------------------------

        segmentLength =
            serializedObject.FindProperty(
                "segmentLength"
            );


        branchRadius =
            serializedObject.FindProperty(
                "branchRadius"
            );


        branchMaterial =
            serializedObject.FindProperty(
                "branchMaterial"
            );


        // -------------------------------------------------------------
        // HEIGHT COLOR
        // -------------------------------------------------------------

        useHeightColor =
            serializedObject.FindProperty(
                "useHeightColor"
            );


        lowColor =
            serializedObject.FindProperty(
                "lowColor"
            );


        highColor =
            serializedObject.FindProperty(
                "highColor"
            );


        colorHeight =
            serializedObject.FindProperty(
                "colorHeight"
            );


        // -------------------------------------------------------------
        // DEBUG
        // -------------------------------------------------------------

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


        // Detectar cualquier modificación realizada
        // desde el Inspector.
        EditorGUI.BeginChangeCheck();


        // =============================================================
        // GENERATION
        // =============================================================

        EditorGUILayout.LabelField(
            "Generation",
            EditorStyles.boldLabel
        );


        EditorGUILayout.PropertyField(
            generationMode,
            new GUIContent(
                "Mode",
                "Selecciona la configuración 2D o 3D."
            )
        );


        EditorGUILayout.PropertyField(
            autoUpdate,
            new GUIContent(
                "Auto Update",
                "Regenera automáticamente el árbol cuando se modifica " +
                "algún parámetro."
            )
        );


        EditorGUILayout.Space(10);


        // =============================================================
        // ACTIVE MODE
        // =============================================================

        LSystemTreeGenerator.GenerationMode mode =
            (LSystemTreeGenerator.GenerationMode)
            generationMode.enumValueIndex;


        if (mode ==
            LSystemTreeGenerator.GenerationMode.TwoD)
        {
            Draw2DConfiguration();
        }
        else
        {
            Draw3DConfiguration();
        }


        EditorGUILayout.Space(10);


        // =============================================================
        // VISUALIZATION
        // =============================================================

        DrawVisualization();


        EditorGUILayout.Space(10);


        // =============================================================
        // HEIGHT COLOR
        // =============================================================

        DrawHeightColor();


        EditorGUILayout.Space(10);


        // =============================================================
        // DEBUG
        // =============================================================

        EditorGUILayout.LabelField(
            "Debug",
            EditorStyles.boldLabel
        );


        EditorGUILayout.PropertyField(
            showDerivation,
            new GUIContent(
                "Show Derivation",
                "Muestra el proceso de expansión del L-System en Console."
            )
        );


        // =============================================================
        // DETECT CHANGES
        // =============================================================

        bool changed =
            EditorGUI.EndChangeCheck();


        serializedObject.ApplyModifiedProperties();


        EditorGUILayout.Space(12);


        // =============================================================
        // TURTLE HELP
        // =============================================================

        DrawSymbolHelp(
            mode
        );


        EditorGUILayout.Space(12);


        // =============================================================
        // BUTTONS
        // =============================================================

        LSystemTreeGenerator generator =
            (LSystemTreeGenerator)target;


        if (GUILayout.Button(
            "Generate Tree",
            GUILayout.Height(35)))
        {
            generator.GenerateTree();

            EditorUtility.SetDirty(
                generator
            );
        }


        if (GUILayout.Button(
            "Clear Generated Tree",
            GUILayout.Height(25)))
        {
            generator.ClearGeneratedTree();

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
            // Se ejecuta en el siguiente ciclo del Editor.
            //
            // Esto evita modificar la jerarquía directamente
            // mientras Unity todavía está dibujando el Inspector.
            EditorApplication.delayCall +=
                () =>
                {
                    if (generator != null)
                    {
                        generator.GenerateTree();

                        EditorUtility.SetDirty(
                            generator
                        );

                        SceneView.RepaintAll();
                    }
                };
        }
    }


    // =====================================================================
    // 2D CONFIGURATION
    // =====================================================================

    private void Draw2DConfiguration()
    {
        EditorGUILayout.LabelField(
            "2D Configuration",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "Configuración base 2D:\n\n" +
            "Axiom: F\n" +
            "F -> F[+F]F[-F]F\n" +
            "Iterations: 3\n" +
            "Angle: 25°",

            MessageType.Info
        );


        EditorGUILayout.PropertyField(
            axiom2D,
            new GUIContent(
                "Axiom",
                "Cadena inicial del L-System."
            )
        );


        EditorGUILayout.PropertyField(
            rules2D,
            new GUIContent(
                "Rules",
                "Reglas de producción."
            ),
            true
        );


        EditorGUILayout.PropertyField(
            iterations2D,
            new GUIContent(
                "Iterations",
                "Cantidad de iteraciones."
            )
        );


        EditorGUILayout.PropertyField(
            angle2D,
            new GUIContent(
                "Angle",
                "Ángulo utilizado por + y -."
            )
        );
    }


    // =====================================================================
    // 3D CONFIGURATION
    // =====================================================================

    private void Draw3DConfiguration()
    {
        EditorGUILayout.LabelField(
            "3D Configuration",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "Configuración base 3D:\n\n" +
            "Axiom: F\n" +
            "F -> F[+F][-F][&F][^F]\n" +
            "Iterations: 3\n" +
            "Angle: 30°\n\n" +
            "+/- generan ramas en un plano.\n" +
            "&/^ generan ramas fuera de ese plano.",

            MessageType.Info
        );


        EditorGUILayout.PropertyField(
            axiom3D,
            new GUIContent(
                "Axiom",
                "Cadena inicial del L-System."
            )
        );


        EditorGUILayout.PropertyField(
            rules3D,
            new GUIContent(
                "Rules",
                "Reglas de producción."
            ),
            true
        );


        EditorGUILayout.PropertyField(
            iterations3D,
            new GUIContent(
                "Iterations",
                "Cantidad de iteraciones."
            )
        );


        EditorGUILayout.PropertyField(
            angle3D,
            new GUIContent(
                "Angle",
                "Ángulo utilizado por las rotaciones 3D."
            )
        );
    }


    // =====================================================================
    // VISUALIZATION
    // =====================================================================

    private void DrawVisualization()
    {
        EditorGUILayout.LabelField(
            "Shared Visualization",
            EditorStyles.boldLabel
        );


        EditorGUILayout.PropertyField(
            segmentLength,
            new GUIContent(
                "Segment Length",
                "Longitud de cada rama."
            )
        );


        EditorGUILayout.PropertyField(
            branchRadius,
            new GUIContent(
                "Branch Radius",
                "Grosor de las ramas."
            )
        );


        EditorGUILayout.PropertyField(
            branchMaterial,
            new GUIContent(
                "Branch Material",
                "Material base utilizado por todas las ramas."
            )
        );
    }


    // =====================================================================
    // HEIGHT COLOR
    // =====================================================================

    private void DrawHeightColor()
    {
        EditorGUILayout.LabelField(
            "Height Color",
            EditorStyles.boldLabel
        );


        EditorGUILayout.PropertyField(
            useHeightColor,
            new GUIContent(
                "Use Height Color",
                "Modifica el color de cada rama según su altura."
            )
        );


        if (!useHeightColor.boolValue)
        {
            return;
        }


        EditorGUI.indentLevel++;


        EditorGUILayout.PropertyField(
            lowColor,
            new GUIContent(
                "Low Color",
                "Color de las zonas inferiores del árbol."
            )
        );


        EditorGUILayout.PropertyField(
            highColor,
            new GUIContent(
                "High Color",
                "Color de las zonas superiores del árbol."
            )
        );


        EditorGUILayout.PropertyField(
            colorHeight,
            new GUIContent(
                "Color Height",
                "Altura en la que se alcanza completamente High Color."
            )
        );


        EditorGUI.indentLevel--;
    }


    // =====================================================================
    // SYMBOL HELP
    // =====================================================================

    private void DrawSymbolHelp(
        LSystemTreeGenerator.GenerationMode mode)
    {
        if (mode ==
            LSystemTreeGenerator.GenerationMode.TwoD)
        {
            EditorGUILayout.HelpBox(
                "Turtle Graphics 2D\n\n" +

                "F = avanzar dibujando\n" +
                "f = avanzar sin dibujar\n\n" +

                "+ = girar\n" +
                "- = girar en sentido contrario\n\n" +

                "[ = guardar posición y orientación\n" +
                "] = recuperar posición y orientación",

                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Turtle Graphics 3D\n\n" +

                "F = avanzar dibujando\n" +
                "f = avanzar sin dibujar\n\n" +

                "+ / - = rotación en un plano\n" +
                "& / ^ = pitch\n" +
                "\\ / / = roll\n\n" +

                "[ = guardar posición y orientación\n" +
                "] = recuperar posición y orientación",

                MessageType.None
            );
        }
    }
}