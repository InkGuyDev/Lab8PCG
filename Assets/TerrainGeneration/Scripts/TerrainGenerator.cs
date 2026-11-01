using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public enum GenerationMethod
    {
        RandomNoise,
        ValueNoise,
        PerlinNoise,
        DiamondSquare
    }

    public enum HeightmapResolution
    {
        R33 = 33,
        R65 = 65,
        R129 = 129,
        R257 = 257,
        R513 = 513
    }

    [Tooltip("Método utilizado para generar las alturas del terreno.")]
    [SerializeField] private GenerationMethod generationMethod = GenerationMethod.RandomNoise;

    [Tooltip("Regenera automáticamente el terreno cada vez que se modifica un parámetro.")]
    [SerializeField] private bool autoUpdate = true;

    [Tooltip("Tamaño físico del terreno sobre el eje X. No modifica la resolución del heightmap.")]
    [Min(1f)]
    [SerializeField] private float terrainWidth = 100f;

    [Tooltip("Tamaño físico del terreno sobre el eje Z. No modifica la resolución del heightmap.")]
    [Min(1f)]
    [SerializeField] private float terrainLength = 100f;

    [Tooltip("Cantidad de muestras utilizadas para representar las alturas. Una resolución mayor permite representar más detalle sin cambiar el tamaño físico del terreno.")]
    [SerializeField] private HeightmapResolution heightmapResolution = HeightmapResolution.R129;

    private const float TERRAIN_HEIGHT = 20f;

    [Tooltip("Semilla utilizada por el generador pseudoaleatorio. Los mismos parámetros y la misma semilla producen el mismo resultado.")]
    [SerializeField] private int seed = 12345;

    [Tooltip("Método utilizado para combinar los valores de los puntos de control en Value Noise.")]
    [SerializeField] private HeightmapGenerator.InterpolationMode valueNoiseInterpolation =
        HeightmapGenerator.InterpolationMode.Bilinear;

    [Tooltip("Distancia, medida en muestras del heightmap, entre los puntos aleatorios utilizados por Value Noise. Valores mayores generan características de mayor escala.")]
    [SerializeField] private int latticeSpacing = 16;

    [Tooltip("Cantidad de variaciones del ruido distribuidas sobre el terreno. Valores mayores producen características más pequeñas y frecuentes.")]
    [Range(0.1f, 20f)]
    [SerializeField] private float perlinFrequency = 4f;

    [Tooltip("Método utilizado para combinar las contribuciones de los gradientes vecinos en Perlin Noise.")]
    [SerializeField] private HeightmapGenerator.InterpolationMode perlinInterpolation =
        HeightmapGenerator.InterpolationMode.Bicubic;

    [Tooltip("Cantidad de subdivisiones realizadas por Diamond-Square. También determina la resolución final mediante 2^iterations + 1.")]
    [Range(5, 9)]
    [SerializeField] private int diamondIterations = 7;

    [Tooltip("Magnitud inicial de la perturbación aleatoria añadida durante los pasos Diamond y Square.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float diamondRoughness = 0.4f;

    [Tooltip("Factor aplicado a Roughness después de cada iteración. Valores altos conservan más irregularidad en las escalas pequeñas.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float diamondRoughnessDecay = 0.5f;

    [Tooltip("Colorea el terreno utilizando bandas definidas según la altura normalizada. Solo afecta la visualización.")]
    [SerializeField] private bool paintByHeight = true;

    [Tooltip("Altura normalizada bajo la cual el terreno utiliza el color de zonas bajas.")]
    [Range(0f, 1f)]
    [SerializeField] private float lowThreshold = 0.35f;

    [Tooltip("Altura normalizada sobre la cual el terreno utiliza el color de zonas altas.")]
    [Range(0f, 1f)]
    [SerializeField] private float highThreshold = 0.7f;

    [Tooltip("Color utilizado para representar las zonas de menor altura.")]
    [SerializeField] private Color lowColor = new Color(0.25f, 0.55f, 0.2f);

    [Tooltip("Color utilizado para representar las zonas de altura intermedia.")]
    [SerializeField] private Color middleColor = new Color(0.45f, 0.3f, 0.15f);

    [Tooltip("Color utilizado para representar las zonas de mayor altura.")]
    [SerializeField] private Color highColor = new Color(0.8f, 0.8f, 0.8f);

    [HideInInspector]
    [SerializeField] private Terrain generatedTerrain;

    private TerrainLayer lowLayer;
    private TerrainLayer middleLayer;
    private TerrainLayer highLayer;

    private const int ALPHAMAP_RESOLUTION = 256;

    public bool AutoUpdate
    {
        get { return autoUpdate; }
    }

    public void GenerateTerrain()
    {
        int resolution = GetCurrentResolution();

        EnsureTerrain(resolution);

        float[,] heights = null;

        switch (generationMethod)
        {
            case GenerationMethod.RandomNoise:
                heights = HeightmapGenerator.GenerateRandomNoise(resolution, seed);
                break;

            case GenerationMethod.ValueNoise:
                heights = HeightmapGenerator.GenerateValueNoise(
                    resolution,
                    latticeSpacing,
                    seed,
                    valueNoiseInterpolation
                );
                break;

            case GenerationMethod.PerlinNoise:
                heights = PerlinNoiseGenerator.GenerateHeightmap(
                    resolution,
                    perlinFrequency,
                    seed,
                    perlinInterpolation
                );
                break;

            case GenerationMethod.DiamondSquare:
                heights = DiamondSquareGenerator.GenerateHeightmap(
                    diamondIterations,
                    seed,
                    diamondRoughness,
                    diamondRoughnessDecay
                );
                break;
        }

        if (heights == null)
        {
            return;
        }

        ApplyHeightmap(heights);

        if (paintByHeight)
        {
            ApplyHeightColors(heights);
        }
        else
        {
            ClearHeightColors();
        }
    }

    public int GetCurrentResolution()
    {
        if (generationMethod == GenerationMethod.DiamondSquare)
        {
            return (1 << diamondIterations) + 1;
        }

        return (int)heightmapResolution;
    }

    private void EnsureTerrain(int resolution)
    {
        if (generatedTerrain == null)
        {
            Transform existingTerrain = transform.Find("Generated Terrain");

            if (existingTerrain != null)
            {
                generatedTerrain = existingTerrain.GetComponent<Terrain>();
            }
        }

        if (generatedTerrain == null)
        {
            CreateTerrain(resolution);
        }

        TerrainData terrainData = generatedTerrain.terrainData;

        if (terrainData.heightmapResolution != resolution)
        {
            terrainData.heightmapResolution = resolution;
        }

        terrainData.size = new Vector3(terrainWidth, TERRAIN_HEIGHT, terrainLength);

        TerrainCollider terrainCollider = generatedTerrain.GetComponent<TerrainCollider>();

        if (terrainCollider != null)
        {
            terrainCollider.terrainData = terrainData;
        }
    }

    private void CreateTerrain(int resolution)
    {
        TerrainData terrainData = new TerrainData();

        terrainData.heightmapResolution = resolution;
        terrainData.size = new Vector3(terrainWidth, TERRAIN_HEIGHT, terrainLength);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);

        terrainObject.name = "Generated Terrain";
        terrainObject.transform.SetParent(transform);
        terrainObject.transform.localPosition = Vector3.zero;

        generatedTerrain = terrainObject.GetComponent<Terrain>();
    }

    private void ApplyHeightmap(float[,] heights)
    {
        if (generatedTerrain == null)
        {
            return;
        }

        generatedTerrain.terrainData.SetHeights(0, 0, heights);
    }

    private void ApplyHeightColors(float[,] heights)
    {
        TerrainData terrainData = generatedTerrain.terrainData;

        EnsureHeightLayers();

        terrainData.terrainLayers = new TerrainLayer[]
        {
            lowLayer,
            middleLayer,
            highLayer
        };

        lowThreshold = Mathf.Clamp01(lowThreshold);
        highThreshold = Mathf.Clamp(highThreshold, lowThreshold + 0.01f, 1f);

        terrainData.alphamapResolution = ALPHAMAP_RESOLUTION;

        float[,,] alphamaps = new float[ALPHAMAP_RESOLUTION, ALPHAMAP_RESOLUTION, 3];
        int heightResolution = heights.GetLength(0);

        for (int y = 0; y < ALPHAMAP_RESOLUTION; y++)
        {
            for (int x = 0; x < ALPHAMAP_RESOLUTION; x++)
            {
                int heightX = Mathf.RoundToInt(
                    x / (float)(ALPHAMAP_RESOLUTION - 1) * (heightResolution - 1)
                );

                int heightY = Mathf.RoundToInt(
                    y / (float)(ALPHAMAP_RESOLUTION - 1) * (heightResolution - 1)
                );

                float height = heights[heightY, heightX];

                if (height < lowThreshold)
                {
                    alphamaps[y, x, 0] = 1f;
                }
                else if (height < highThreshold)
                {
                    alphamaps[y, x, 1] = 1f;
                }
                else
                {
                    alphamaps[y, x, 2] = 1f;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    private void ClearHeightColors()
    {
        if (generatedTerrain == null)
        {
            return;
        }

        generatedTerrain.terrainData.terrainLayers = new TerrainLayer[0];
    }

    private void EnsureHeightLayers()
    {
        if (lowLayer == null)
        {
            lowLayer = CreateTerrainLayer("Low", lowColor);
        }
        else
        {
            UpdateTerrainLayerColor(lowLayer, lowColor);
        }

        if (middleLayer == null)
        {
            middleLayer = CreateTerrainLayer("Middle", middleColor);
        }
        else
        {
            UpdateTerrainLayerColor(middleLayer, middleColor);
        }

        if (highLayer == null)
        {
            highLayer = CreateTerrainLayer("High", highColor);
        }
        else
        {
            UpdateTerrainLayerColor(highLayer, highColor);
        }
    }

    private TerrainLayer CreateTerrainLayer(string layerName, Color color)
    {
        TerrainLayer layer = new TerrainLayer();

        layer.name = layerName;
        layer.hideFlags = HideFlags.HideAndDontSave;
        layer.diffuseTexture = CreateColorTexture(color);
        layer.tileSize = new Vector2(10f, 10f);

        return layer;
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);

        texture.name = "Generated Height Color";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();

        return texture;
    }

    private void UpdateTerrainLayerColor(TerrainLayer layer, Color color)
    {
        Texture2D texture = layer.diffuseTexture as Texture2D;

        if (texture == null)
        {
            layer.diffuseTexture = CreateColorTexture(color);
            return;
        }

        texture.SetPixel(0, 0, color);
        texture.Apply();
    }

    public void DeleteTerrain()
    {
        if (generatedTerrain == null)
        {
            Transform existingTerrain = transform.Find("Generated Terrain");

            if (existingTerrain != null)
            {
                generatedTerrain = existingTerrain.GetComponent<Terrain>();
            }
        }

        if (generatedTerrain == null)
        {
            return;
        }

        GameObject terrainObject = generatedTerrain.gameObject;
        generatedTerrain = null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(terrainObject);
        }
        else
        {
            Destroy(terrainObject);
        }
#else
        Destroy(terrainObject);
#endif
    }
}
