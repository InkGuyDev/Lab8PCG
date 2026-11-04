using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Laboratorios.Lab10.WaveFunctionCollapse
{
    /*
     * ========================================================
     * LABORATORIO 8 - WFC II: VISUALIZACION Y COMPARACION
     * ========================================================
     *
     * Este componente se entrega TOTALMENTE IMPLEMENTADO.
     * No se requiere modificarlo para completar el laboratorio.
     *
     * Presenta la matriz de entrenamiento y dos generaciones:
     * una con 4 vecinos (Cardinal) y otra con 8 (Moore).
     * Permite comparar frecuencias, probabilidades direccionales,
     * semillas y resultados, ademas de exportar la evidencia.
     *
     * El componente utiliza las propiedades y funciones publicas
     * de WFCGenerator y WFCTileSet: mantener sus firmas intactas.
     */

    // Dos instancias del mismo generador; este componente solo coordina y visualiza.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(WFCGenerator))]
    public class WFCDebugger : MonoBehaviour
    {
        [SerializeField] private Camera mapCamera;
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.16f, 0.20f);
        [SerializeField] private bool showDomains = true;
        [SerializeField] private bool showStatistics = false;

        private class MapView
        {
            public WFCGenerator generator;
            public Transform root;
            public GameObject[,] cells;
            public int[,] displayed;
            public int revision = -1, generation = -1;
            public int[] counts;
        }
        private WFCGenerator generator, mooreGenerator;
        private MapView[] maps;
        private Transform trainingRoot;
        private int trainingVersion = -1;
        private string widthText, heightText, seedText, matrixText = "", message = "";
        private bool fixSeed, editMatrix, tileMenu, directionMenu, framed;
        private int selectedTile, selectedDirection;
        private Vector2 statisticsScroll, matrixScroll;
        private GUIStyle heading, text, small, mapLabel;
        private Rect viewport;
        private float scale, canvasWidth, canvasHeight;
        public WFCGenerator CardinalGenerator => generator;
        public WFCGenerator MooreGenerator => mooreGenerator;
        public int SharedSeed => generator.UsedSeed;
        public bool ShowStatistics => showStatistics;
        public string LastExportDirectory { get; private set; }

        private void Awake()
        {
            generator = GetComponent<WFCGenerator>();
            if (mapCamera == null) mapCamera = Camera.main;
            if (mapCamera != null)
            {
                mapCamera.orthographic = true;
                mapCamera.clearFlags = CameraClearFlags.SolidColor;
                mapCamera.backgroundColor = backgroundColor;
            }
            widthText = generator.Width.ToString();
            heightText = generator.Height.ToString();
            seedText = generator.Seed.ToString();
            fixSeed = !generator.RandomSeed;
            maps = new[] { new MapView { generator = generator }, new MapView() };
        }
        private void Start()
        {
            if (generator.TileSet.HasSample)
                GenerateComparison(generator.Width, generator.Height, fixSeed, generator.Seed, false);
        }
        public bool GenerateComparison(int width, int height, bool useFixedSeed, int seed, bool run = true)
        {
            if (!generator.TileSet.HasSample) { message = "Primero aprende una matriz."; return false; }
            int shared = useFixedSeed ? seed : Guid.NewGuid().GetHashCode();
            if (!useFixedSeed && shared == generator.UsedSeed) shared = unchecked(shared + 1);
            bool resize = width != generator.Width || height != generator.Height;
            if (!generator.Configure(width, height, 4, false, shared)) { message = generator.Status; return false; }
            if (mooreGenerator == null)
            {
                var go = new GameObject("WFC_8_Vecinos");
                go.transform.SetParent(transform, false);
                mooreGenerator = go.AddComponent<WFCGenerator>();
                maps[1].generator = mooreGenerator;
            }
            mooreGenerator.SetAutomatic(false);
            if (!generator.Initialize() || !mooreGenerator.InitializeComparison(generator, 8, shared))
            { message = generator.Status + " / " + mooreGenerator.Status; return false; }
            fixSeed = useFixedSeed;
            widthText = width.ToString(); heightText = height.ToString(); seedText = shared.ToString();
            message = "";
            SetAutomatic(run);
            if (resize && framed) FrameAll();
            return true;
        }
        public void SetAutomatic(bool enabled)
        {
            generator.SetAutomatic(enabled);
            if (mooreGenerator != null) mooreGenerator.SetAutomatic(enabled);
        }
        private void OnDisable() { if (generator != null) SetAutomatic(false); }
        private void LateUpdate()
        {
            UpdateViewport();
            if (!generator.TileSet.HasSample) return;
            RefreshTraining();
            foreach (MapView map in maps)
                if (map.generator != null && map.revision != map.generator.Revision) RefreshMap(map);
            PositionMaps();
            if (!framed && generator.IsInitialized && mooreGenerator != null)
            { FrameAll(); framed = true; }
        }

        // REPRESENTACION: entrenamiento, 4 vecinos y 8 vecinos en una sola fila.
        private static void RemoveRoot(ref Transform root)
        {
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            root = null;
        }
        private Transform NewRoot(string label)
        {
            var root = new GameObject(label).transform;
            root.SetParent(transform, false);
            return root;
        }
        private GameObject CreateTile(int tile, Transform root, int x, int y)
        {
            GameObject prefab = generator.TileSet.Tiles[tile].prefab;
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, root);
            go.name = generator.TileSet.Tiles[tile].id + $"_{x}_{y}";
            go.transform.localPosition = new Vector3(x, y, 0) * generator.CellSize;
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                Vector2 size = renderer.sprite.bounds.size;
                go.transform.localScale = new Vector3(generator.CellSize * 0.96f / size.x, generator.CellSize * 0.96f / size.y, 1);
            }
            return go;
        }
        private void RefreshTraining()
        {
            if (trainingVersion == generator.TrainingVersion) return;
            trainingVersion = generator.TrainingVersion;
            RemoveRoot(ref trainingRoot);
            trainingRoot = NewRoot("Entrenamiento");
            var set = generator.TileSet;
            for (int x = 0; x < set.Width; x++)
                for (int y = 0; y < set.Height; y++) CreateTile(set.Sample[x, y], trainingRoot, x, y);
        }
        private void RefreshMap(MapView map)
        {
            var g = map.generator;
            if (!g.IsInitialized || map.generation != g.GenerationVersion)
            {
                RemoveRoot(ref map.root); map.cells = null;
                map.generation = g.GenerationVersion;
                if (g.IsInitialized)
                {
                    map.root = NewRoot("Generacion_" + g.Connections + "_Vecinos");
                    map.cells = new GameObject[g.Width, g.Height];
                    map.displayed = new int[g.Width, g.Height];
                    for (int x = 0; x < g.Width; x++)
                        for (int y = 0; y < g.Height; y++) map.displayed[x, y] = int.MinValue;
                }
            }
            map.counts = g.GetResolvedCounts(); map.revision = g.Revision;
            if (map.cells == null) return;
            for (int x = 0; x < g.Width; x++)
                for (int y = 0; y < g.Height; y++)
                {
                    int tile = g.GetResolvedTile(x, y);
                    int visual = g.GetDomainCount(x, y) == 0 ? -2 : tile;
                    if (visual == map.displayed[x, y]) continue;
                    map.displayed[x, y] = visual;
                    if (map.cells[x, y] != null) { map.cells[x, y].SetActive(false); Destroy(map.cells[x, y]); }
                    map.cells[x, y] = CreateTile(Math.Max(0, tile), map.root, x, y);
                    if (tile < 0 && map.cells[x, y] != null)
                        map.cells[x, y].GetComponent<SpriteRenderer>().color = visual == -2
                            ? new Color(0.95f, 0.15f, 0.2f) : new Color(0.25f, 0.28f, 0.32f);
                }
        }
        private float TotalRows => Mathf.Max(generator.TileSet.Height, generator.Height);
        private void PositionMaps()
        {
            float unit = generator.CellSize;
            if (trainingRoot != null) trainingRoot.localPosition = Vector3.up * ((TotalRows - generator.TileSet.Height) * 0.5f * unit);
            float baseY = (TotalRows - generator.Height) * 0.5f;
            for (int i = 0; i < maps.Length; i++)
                if (maps[i].root != null) maps[i].root.localPosition = new Vector3(
                    generator.TileSet.Width + 4 + i * (generator.Width + 4), baseY, 0) * unit;
        }
        private void UpdateViewport()
        {
            scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 760f));
            canvasWidth = Screen.width / scale; canvasHeight = Screen.height / scale;
            viewport = new Rect(12, 112, canvasWidth - 24 - (showStatistics ? 344 : 0), canvasHeight - 146);
            if (mapCamera != null) mapCamera.rect = new Rect(viewport.x / canvasWidth, 1 - viewport.yMax / canvasHeight,
                viewport.width / canvasWidth, viewport.height / canvasHeight);
        }

        // TODOS LOS CONTROLES EN UNA BARRA. Las estadisticas son opcionales.
        private void OnGUI()
        {
            if (!Application.isPlaying || mapCamera == null) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
                small = new GUIStyle(text) { fontSize = 12 };
                mapLabel = new GUIStyle(GUI.skin.box) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            UpdateViewport();
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            Color before = GUI.color; GUI.color = new Color(0.075f, 0.095f, 0.125f);
            GUI.DrawTexture(new Rect(0, 0, canvasWidth, viewport.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, viewport.y, viewport.x, canvasHeight - viewport.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(viewport.xMax, viewport.y, canvasWidth - viewport.xMax, canvasHeight - viewport.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(viewport.x, viewport.yMax, viewport.width, canvasHeight - viewport.yMax), Texture2D.whiteTexture);
            GUI.color = before;
            GUILayout.BeginArea(new Rect(12, 8, canvasWidth - 24, 98));
            GUILayout.BeginHorizontal();
            GUILayout.Label("LAB 10 | Comparar WFC: 4 y 8 vecinos", heading);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Editar matriz", GUILayout.Width(124)))
            { editMatrix = !editMatrix; matrixText = generator.TrainingText; SetAutomatic(false); }
            if (GUILayout.Button(showStatistics ? "Ocultar probabilidades" : "Probabilidades", GUILayout.Width(164)))
            { showStatistics = !showStatistics; UpdateViewport(); FrameAll(); }
            if (GUILayout.Button("Encuadrar", GUILayout.Width(96))) FrameAll();
            GUI.enabled = generator.TileSet.HasSample;
            if (GUILayout.Button("Exportar ambos", GUILayout.Width(120))) Export();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            CompactField("Ancho", ref widthText); CompactField("Alto", ref heightText);
            fixSeed = GUILayout.Toggle(fixSeed, "Fijar semilla", GUILayout.Width(112));
            if (fixSeed)
            {
                GUI.SetNextControlName("WFC_Input_Semilla");
                seedText = GUILayout.TextField(seedText, 12, GUILayout.Width(116));
            }
            else GUILayout.Label("Aleatoria en cada generacion", small, GUILayout.Width(194));
            GUI.enabled = generator.TileSet.HasSample && !editMatrix;
            if (GUILayout.Button("Generar ambos", GUILayout.Width(166), GUILayout.Height(29)) && TrySettings(out int w, out int h, out int seed))
                GenerateComparison(w, h, fixSeed, seed);
            GUI.enabled = !editMatrix && (CanRun(generator) || CanRun(mooreGenerator));
            bool automatic = generator.Automatic || (mooreGenerator != null && mooreGenerator.Automatic);
            if (GUILayout.Button(automatic ? "Pausar" : "Continuar", GUILayout.Width(98), GUILayout.Height(29))) SetAutomatic(!automatic);
            GUI.enabled = true;
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Label(generator.IsInitialized ? $"Semilla compartida: {SharedSeed} | {generator.Width} x {generator.Height} | Entrenamiento / 4 vecinos / 8 Moore" : generator.Status, small);
            GUILayout.EndArea();
            DrawMapLabels();
            if (showStatistics)
            {
                GUILayout.BeginArea(new Rect(canvasWidth - 332, 112, 320, canvasHeight - 146), GUI.skin.box);
                statisticsScroll = GUILayout.BeginScrollView(statisticsScroll);
                if (generator.TileSet.HasSample) DrawStatistics();
                GUILayout.EndScrollView(); GUILayout.EndArea();
            }
            GUI.Label(new Rect(12, canvasHeight - 27, canvasWidth - 24, 24), string.IsNullOrEmpty(message)
                ? "Arrastre derecho/central: mover | Rueda: zoom | Gris: pendiente | Rojo: contradiccion" : message, small);
            if (editMatrix) DrawMatrixEditor();
            GUI.matrix = previous;
            if (!editMatrix) Navigate();
        }
        private static bool CanRun(WFCGenerator g) => g != null && g.IsInitialized && !g.Finished;
        private void CompactField(string label, ref string value)
        {
            GUILayout.Label(label, GUILayout.Width(42));
            GUI.SetNextControlName("WFC_Input_" + label);
            value = GUILayout.TextField(value ?? "", 4, GUILayout.Width(52));
        }
        private bool TrySettings(out int width, out int height, out int seed)
        {
            seed = generator.Seed;
            bool valid = int.TryParse(widthText, out width);
            valid &= int.TryParse(heightText, out height);
            if (fixSeed) valid &= int.TryParse(seedText, out seed);
            if (!valid || width < 1 || height < 1 || width > 128 || height > 128 || (long)width * height > 4096)
            { message = "Ancho/alto: 1 a 128, hasta 4096 celdas por mapa. Semilla fija: entero valido."; return false; }
            return true;
        }
        private void DrawMatrixEditor()
        {
            GUILayout.BeginArea(viewport, GUI.skin.box);
            GUILayout.Label("EDITAR ENTRENAMIENTO PARA AMBAS VERSIONES", heading);
            GUILayout.Label("P: pasto | R: arena | A: agua | B: bosque. Primera fila arriba, filas del mismo largo.", text);
            matrixScroll = GUILayout.BeginScrollView(matrixScroll);
            GUI.SetNextControlName("WFC_Input_Matriz");
            matrixText = GUILayout.TextArea(matrixText, GUILayout.MinHeight(viewport.height - 140));
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Aprender y preparar ambos") && TrySettings(out int w, out int h, out int seed))
            {
                if (generator.LoadTraining(matrixText))
                { editMatrix = false; GenerateComparison(w, h, fixSeed, seed, false); FrameAll(); }
                else message = generator.Status;
            }
            if (GUILayout.Button("Cancelar")) { editMatrix = false; message = ""; }
            GUILayout.EndHorizontal(); GUILayout.Label(message, small); GUILayout.EndArea();
        }
        private void DrawMapLabels()
        {
            GUI.BeginGroup(viewport);
            DrawLabel(trainingRoot, generator.TileSet.Width, generator.TileSet.Height, "ENTRENAMIENTO", $"{generator.TileSet.Width} x {generator.TileSet.Height}");
            foreach (MapView map in maps)
                if (map.generator != null && map.generator.IsInitialized)
                {
                    var g = map.generator;
                    int resolved = 0;
                    if (map.counts != null) foreach (int count in map.counts) resolved += count;
                    string state = g.Phase == WFCPhase.Completed ? "Completado" : g.Finished ? g.Status : g.Automatic ? "Generando" : "En pausa";
                    DrawLabel(map.root, g.Width, g.Height, g.Connections == 4 ? "4 VECINOS" : "8 VECINOS / MOORE", $"{state} | {resolved}/{g.Width * g.Height}");
                }
            GUI.EndGroup();
        }
        private void DrawLabel(Transform root, int width, int height, string title, string detail)
        {
            if (root == null) return;
            Vector3 screen = mapCamera.WorldToScreenPoint(root.TransformPoint(new Vector3((width - 1) * 0.5f, height + 0.7f, 0) * generator.CellSize));
            float x = screen.x / scale - viewport.x, y = (Screen.height - screen.y) / scale - viewport.y;
            if (screen.z > 0) GUI.Box(new Rect(x - 136, y - 25, 272, 46), title + "\n" + detail, mapLabel);
        }
        private void Dropdown(string[] options, ref int selected, ref bool open)
        {
            selected = Mathf.Clamp(selected, 0, options.Length - 1);
            if (GUILayout.Button(options[selected] + (open ? "  ^" : "  v"))) open = !open;
            if (open)
                for (int i = 0; i < options.Length; i++)
                    if (GUILayout.Button(options[i])) { selected = i; open = false; }
        }
        private void DrawStatistics()
        {
            var set = generator.TileSet;
            GUILayout.Label("PORCENTAJES", heading);
            GUILayout.Label("P: pasto | R: arena | A: agua | B: bosque", small);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tile", GUILayout.Width(20)); GUILayout.Label("Original", GUILayout.Width(74));
            GUILayout.Label("4 vecinos", GUILayout.Width(84)); GUILayout.Label("8 Moore", GUILayout.Width(84)); GUILayout.EndHorizontal();
            for (int t = 0; t < set.Tiles.Count; t++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(set.Tiles[t].id, GUILayout.Width(20));
                GUILayout.Label($"{set.Frequencies[t]} ({set.Global(t):P1})", small, GUILayout.Width(74));
                foreach (MapView map in maps)
                {
                    int count = map.counts == null ? 0 : map.counts[t];
                    int total = map.generator != null && map.generator.IsInitialized ? map.generator.Width * map.generator.Height : 0;
                    GUILayout.Label(total == 0 ? "--" : $"{count} ({(double)count / total:P1})", small, GUILayout.Width(84));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Los porcentajes incluyen las celdas pendientes en el total.", small);
            GUILayout.Space(12); GUILayout.Label("PESOS DIRECCIONALES", heading);
            var names = new string[set.Tiles.Count];
            for (int t = 0; t < names.Length; t++) names[t] = set.Tiles[t].id;
            GUILayout.Label("Tile de origen", small); Dropdown(names, ref selectedTile, ref tileMenu);
            GUILayout.Label("Direccion hacia su vecino", small); Dropdown(WFCTileSet.Directions, ref selectedDirection, ref directionMenu);
            int denominator = set.Total(selectedDirection, selectedTile);
            GUILayout.Label($"P(vecino | {set.Tiles[selectedTile].id}, {WFCTileSet.Directions[selectedDirection]})", heading);
            for (int t = 0; t < names.Length; t++)
                GUILayout.Label($"{names[t]}: {set.Counts[selectedDirection, selectedTile, t]} / {denominator} = {set.Weights[selectedDirection, selectedTile, t]:P2}", text);
            GUILayout.Label("El entrenamiento es compartido. Las diagonales se aplican solo a Moore.", small);
        }

        // CAMARA Y EXPORTACION COMUNES
        public void FrameAll()
        {
            if (mapCamera == null || generator == null || !generator.TileSet.HasSample) return;
            UpdateViewport();
            float columns = generator.TileSet.Width + 8 + generator.Width * 2;
            Vector3 center = transform.TransformPoint(new Vector3((columns - 1) / 2, (TotalRows - 1) / 2, 0) * generator.CellSize);
            mapCamera.transform.position = new Vector3(center.x, center.y, center.z - 10);
            float aspect = Mathf.Max(0.1f, Screen.width * mapCamera.rect.width / (Screen.height * mapCamera.rect.height));
            mapCamera.orthographicSize = Mathf.Max(TotalRows * Mathf.Abs(transform.lossyScale.y), columns * Mathf.Abs(transform.lossyScale.x) / aspect) * generator.CellSize * 0.56f;
        }
        private void Navigate()
        {
            Event e = Event.current; Rect pixels = mapCamera.pixelRect;
            bool overMap = new Rect(pixels.x, Screen.height - pixels.yMax, pixels.width, pixels.height).Contains(e.mousePosition);
            if (overMap && e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2))
            { mapCamera.transform.position += new Vector3(-e.delta.x, e.delta.y, 0) * mapCamera.orthographicSize * 2 / pixels.height; e.Use(); }
            else if (overMap && e.type == EventType.ScrollWheel)
            { mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize * Mathf.Exp(e.delta.y * 0.07f), 0.5f, 500); e.Use(); }
            else if (e.type == EventType.KeyDown && !GUI.GetNameOfFocusedControl().StartsWith("WFC_Input"))
            {
                Vector3 direction = e.keyCode == KeyCode.LeftArrow ? Vector3.left : e.keyCode == KeyCode.RightArrow ? Vector3.right
                    : e.keyCode == KeyCode.UpArrow ? Vector3.up : e.keyCode == KeyCode.DownArrow ? Vector3.down : Vector3.zero;
                if (direction != Vector3.zero) { mapCamera.transform.position += direction * mapCamera.orthographicSize * 0.1f; e.Use(); }
            }
        }
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showDomains || maps == null) return;
            foreach (MapView map in maps)
            {
                if (map.root == null || !map.generator.IsInitialized) continue;
                for (int x = 0; x < map.generator.Width; x++)
                    for (int y = 0; y < map.generator.Height; y++)
                    {
                        Vector3 p = map.root.TransformPoint(new Vector3(x, y, 0) * generator.CellSize);
                        Gizmos.color = map.generator.LastChanged == new Vector2Int(x, y) ? Color.yellow : new Color(1, 1, 1, 0.2f);
                        Gizmos.DrawWireCube(p, new Vector3(generator.CellSize, generator.CellSize, 0.01f));
                    }
            }
        }
        public void Export()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "Lab10", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "entrenamiento.csv"), generator.TrainingText, Encoding.UTF8);
                File.WriteAllText(Path.Combine(path, "aprendizaje.csv"), generator.TileSet.StatisticsCsv(), Encoding.UTF8);
                var execution = new StringBuilder($"Semilla compartida: {SharedSeed}\nSemilla fija: {fixSeed}\nTamano: {generator.Width}x{generator.Height}\n");
                var comparison = new StringBuilder("tile,original,probabilidad_original,conteo_4,proporcion_4,conteo_8,proporcion_8\n");
                foreach (MapView map in maps)
                    if (map.generator != null && map.generator.IsInitialized)
                    {
                        var g = map.generator;
                        File.WriteAllText(Path.Combine(path, "mapa_" + g.Connections + ".csv"), g.MapCsv(), Encoding.UTF8);
                        execution.AppendLine($"{g.Connections} vecinos: {g.Phase}, semilla {g.UsedSeed}, observaciones {g.Observations}, retrocesos {g.Backtracks}.");
                    }
                var counts4 = generator.GetResolvedCounts();
                var counts8 = mooreGenerator == null ? new int[counts4.Length] : mooreGenerator.GetResolvedCounts();
                for (int t = 0; t < counts4.Length; t++)
                {
                    double total = generator.Width * generator.Height;
                    comparison.AppendLine($"{generator.TileSet.Tiles[t].id},{generator.TileSet.Frequencies[t]},{generator.TileSet.Global(t).ToString("R", CultureInfo.InvariantCulture)},{counts4[t]},{(counts4[t]/total).ToString("R", CultureInfo.InvariantCulture)},{counts8[t]},{(counts8[t]/total).ToString("R", CultureInfo.InvariantCulture)}");
                }
                File.WriteAllText(Path.Combine(path, "ejecucion.txt"), execution.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(path, "comparacion.csv"), comparison.ToString(), Encoding.UTF8);
                LastExportDirectory = path; message = "Exportado: " + path; Debug.Log(message, this);
            }
            catch (Exception ex) { message = "Error al exportar: " + ex.Message; }
        }
    }
}
