using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    private SerializedProperty generationMethod;
    private SerializedProperty autoUpdate;

    private SerializedProperty terrainWidth;
    private SerializedProperty terrainLength;
    private SerializedProperty heightmapResolution;

    private SerializedProperty seed;

    private SerializedProperty valueNoiseInterpolation;
    private SerializedProperty latticeSpacing;

    private SerializedProperty perlinFrequency;
    private SerializedProperty perlinInterpolation;

    private SerializedProperty diamondIterations;
    private SerializedProperty diamondRoughness;
    private SerializedProperty diamondRoughnessDecay;

    private SerializedProperty paintByHeight;
    private SerializedProperty lowThreshold;
    private SerializedProperty highThreshold;
    private SerializedProperty lowColor;
    private SerializedProperty middleColor;
    private SerializedProperty highColor;

    private void OnEnable()
    {
        generationMethod = serializedObject.FindProperty("generationMethod");
        autoUpdate = serializedObject.FindProperty("autoUpdate");

        terrainWidth = serializedObject.FindProperty("terrainWidth");
        terrainLength = serializedObject.FindProperty("terrainLength");
        heightmapResolution = serializedObject.FindProperty("heightmapResolution");

        seed = serializedObject.FindProperty("seed");

        valueNoiseInterpolation = serializedObject.FindProperty("valueNoiseInterpolation");
        latticeSpacing = serializedObject.FindProperty("latticeSpacing");

        perlinFrequency = serializedObject.FindProperty("perlinFrequency");
        perlinInterpolation = serializedObject.FindProperty("perlinInterpolation");

        diamondIterations = serializedObject.FindProperty("diamondIterations");
        diamondRoughness = serializedObject.FindProperty("diamondRoughness");
        diamondRoughnessDecay = serializedObject.FindProperty("diamondRoughnessDecay");

        paintByHeight = serializedObject.FindProperty("paintByHeight");
        lowThreshold = serializedObject.FindProperty("lowThreshold");
        highThreshold = serializedObject.FindProperty("highThreshold");

        lowColor = serializedObject.FindProperty("lowColor");
        middleColor = serializedObject.FindProperty("middleColor");
        highColor = serializedObject.FindProperty("highColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Terrain Generation", EditorStyles.boldLabel);

        DrawProperty(generationMethod, "Generation Method");
        DrawProperty(autoUpdate, "Auto Update");

        EditorGUILayout.Space(10);

        DrawTerrainSettings();

        EditorGUILayout.Space(10);

        DrawAlgorithmSettings();

        EditorGUILayout.Space(10);

        DrawHeightVisualization();

        bool changed = EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();

        TerrainGenerator generator = (TerrainGenerator)target;

        if (changed && generator.AutoUpdate)
        {
            generator.GenerateTerrain();
            EditorUtility.SetDirty(generator);
        }

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Generate Terrain", GUILayout.Height(30)))
        {
            generator.GenerateTerrain();
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Delete Generated Terrain"))
        {
            generator.DeleteTerrain();
            EditorUtility.SetDirty(generator);
        }
    }

    private void DrawTerrainSettings()
    {
        EditorGUILayout.LabelField("Terrain Settings", EditorStyles.boldLabel);

        DrawProperty(terrainWidth, "Width");
        DrawProperty(terrainLength, "Length");

        TerrainGenerator.GenerationMethod method =
            (TerrainGenerator.GenerationMethod)generationMethod.enumValueIndex;

        if (method != TerrainGenerator.GenerationMethod.DiamondSquare)
        {
            DrawProperty(heightmapResolution, "Heightmap Resolution");
        }
    }

    private void DrawAlgorithmSettings()
    {
        TerrainGenerator.GenerationMethod method =
            (TerrainGenerator.GenerationMethod)generationMethod.enumValueIndex;

        switch (method)
        {
            case TerrainGenerator.GenerationMethod.RandomNoise:
                DrawRandomNoiseSettings();
                break;

            case TerrainGenerator.GenerationMethod.ValueNoise:
                DrawValueNoiseSettings();
                break;

            case TerrainGenerator.GenerationMethod.PerlinNoise:
                DrawPerlinNoiseSettings();
                break;

            case TerrainGenerator.GenerationMethod.DiamondSquare:
                DrawDiamondSquareSettings();
                break;
        }
    }

    private void DrawRandomNoiseSettings()
    {
        EditorGUILayout.LabelField("Random Noise", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Asigna una altura aleatoria independiente a cada posición del heightmap. " +
            "Permite observar por qué la aleatoriedad sin relación espacial produce terrenos poco naturales.",
            MessageType.Info
        );

        DrawProperty(seed, "Seed");
    }

    private void DrawValueNoiseSettings()
    {
        EditorGUILayout.LabelField("Value Noise", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Genera valores aleatorios sobre una grilla de menor resolución y calcula los valores intermedios. " +
            "Compare None, Bilinear y Bicubic para observar cómo cambia la continuidad del terreno.",
            MessageType.Info
        );

        DrawProperty(seed, "Seed");
        DrawProperty(valueNoiseInterpolation, "Interpolation");

        DrawLatticeSpacing();
    }

    private void DrawPerlinNoiseSettings()
    {
        EditorGUILayout.LabelField("Perlin Noise", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Genera gradientes pseudoaleatorios y calcula su influencia mediante productos punto. " +
            "Las contribuciones de los cuatro gradientes vecinos se combinan mediante interpolación.",
            MessageType.Info
        );

        DrawProperty(seed, "Seed");
        DrawProperty(perlinFrequency, "Frequency");

        DrawPerlinInterpolation();
    }

    private void DrawDiamondSquareSettings()
    {
        EditorGUILayout.LabelField("Diamond-Square", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Genera terreno fractal alternando los pasos Diamond y Square. " +
            "Cada iteración trabaja sobre una escala menor y reduce la magnitud de las perturbaciones mediante Roughness Decay.",
            MessageType.Info
        );

        DrawProperty(seed, "Seed");
        DrawProperty(diamondIterations, "Iterations");
        DrawProperty(diamondRoughness, "Roughness");
        DrawProperty(diamondRoughnessDecay, "Roughness Decay");

        int resolution = (1 << diamondIterations.intValue) + 1;

        EditorGUILayout.LabelField(
            new GUIContent(
                "Resulting Resolution",
                "Resolución del heightmap obtenida mediante 2^iterations + 1."
            ),
            new GUIContent(resolution + " x " + resolution)
        );
    }

    private void DrawPerlinInterpolation()
    {
        string[] options =
        {
            "Bilinear",
            "Bicubic"
        };

        int currentIndex =
            perlinInterpolation.enumValueIndex ==
            (int)HeightmapGenerator.InterpolationMode.Bicubic
            ? 1
            : 0;

        int selectedIndex = EditorGUILayout.Popup(
            new GUIContent(
                "Interpolation",
                perlinInterpolation.tooltip
            ),
            currentIndex,
            options
        );

        if (selectedIndex == 0)
        {
            perlinInterpolation.enumValueIndex =
                (int)HeightmapGenerator.InterpolationMode.Bilinear;
        }
        else
        {
            perlinInterpolation.enumValueIndex =
                (int)HeightmapGenerator.InterpolationMode.Bicubic;
        }
    }

    private void DrawHeightVisualization()
    {
        EditorGUILayout.LabelField("Height Visualization", EditorStyles.boldLabel);

        DrawProperty(paintByHeight, "Paint By Height");

        if (!paintByHeight.boolValue)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "La visualización por colores no modifica el algoritmo de generación. " +
            "Solo permite distinguir visualmente diferentes rangos de altura.",
            MessageType.None
        );

        DrawProperty(lowThreshold, "Low Threshold");
        DrawProperty(highThreshold, "High Threshold");

        if (highThreshold.floatValue <= lowThreshold.floatValue)
        {
            highThreshold.floatValue =
                Mathf.Min(1f, lowThreshold.floatValue + 0.01f);
        }

        EditorGUILayout.Space(5);

        DrawProperty(lowColor, "Low Color");
        DrawProperty(middleColor, "Middle Color");
        DrawProperty(highColor, "High Color");
    }

    private void DrawLatticeSpacing()
    {
        int resolution = GetSelectedHeightmapResolution();
        int baseResolution = resolution - 1;

        List<int> values = new List<int>();
        List<string> labels = new List<string>();

        int value = 1;

        while (value <= baseResolution)
        {
            if (baseResolution % value == 0)
            {
                values.Add(value);
                labels.Add(value.ToString());
            }

            value *= 2;
        }

        if (!values.Contains(latticeSpacing.intValue))
        {
            latticeSpacing.intValue = Mathf.Min(16, baseResolution);
        }

        int currentIndex = values.IndexOf(latticeSpacing.intValue);

        int newIndex = EditorGUILayout.Popup(
            new GUIContent(
                "Lattice Spacing",
                latticeSpacing.tooltip
            ),
            currentIndex,
            labels.ToArray()
        );

        if (newIndex >= 0)
        {
            latticeSpacing.intValue = values[newIndex];
        }
    }

    private int GetSelectedHeightmapResolution()
    {
        switch (heightmapResolution.enumValueIndex)
        {
            case 0:
                return 33;
            case 1:
                return 65;
            case 2:
                return 129;
            case 3:
                return 257;
            case 4:
                return 513;
            default:
                return 129;
        }
    }

    private void DrawProperty(SerializedProperty property, string label)
    {
        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label, property.tooltip)
        );
    }
}
