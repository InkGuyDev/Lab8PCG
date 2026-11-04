
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Laboratorios.Lab10.WaveFunctionCollapse
{
    /*
     * ========================================================
     * LABORATORIO 8 - WFC II: GENERACION CON APRENDIZAJE
     * ========================================================
     *
     * CONTINUIDAD DEL LABORATORIO ANTERIOR:
     * Se mantienen dominios, observacion, entropia, propagacion
     * e identificacion de contradicciones de WFC.
     *
     * NOVEDADES:
     * 1. Las compatibilidades se aprenden desde una matriz TXT/CSV.
     * 2. La seleccion utiliza probabilidades aprendidas.
     * 3. Se comparan vecindarios Cardinal (4) y Moore (8).
     * 4. Una contradiccion puede recuperarse mediante backtracking.
     *
     * ESTRUCTURAS PRINCIPALES:
     *   domains[x,y]       -> alternativas disponibles por celda.
     *   compatible[d,a,b]  -> relaciones permitidas por direccion.
     *   pendingArcs        -> relaciones pendientes de propagacion.
     *   decisions          -> historial de elecciones recuperables.
     *
     * El WFCDebugger controla ambas versiones del generador con
     * el mismo entrenamiento y una semilla compartida.
     *
     * NO cambiar las firmas ni los campos publicos: son utilizados
     * por la escena y por el componente WFCDebugger.
     */

    public enum WFCPhase { NotInitialized, ReadyToObserve, Propagating, Completed, Contradiction, Failed, LimitReached }
    public enum WFCNeighborhood { Cardinal = 4, Moore = 8 }

    public class WFCGenerator : MonoBehaviour
    {
        [Header("TileSet y entrenamiento")]
        [SerializeField] private WFCTileSet tileSet = new WFCTileSet();
        [SerializeField] private TextAsset trainingMatrix;

        [Header("Mapa")]
        [SerializeField, Min(1)] private int width = 20;
        [SerializeField, Min(1)] private int height = 20;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField] private WFCNeighborhood neighborhood = WFCNeighborhood.Cardinal;

        [Header("Generacion")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 12345;
        [SerializeField, Min(0)] private int maxBacktracks = 10000;
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField, Min(0.01f)] private float automaticStepDelay = 0.05f;
        [SerializeField, Range(1, 100)] private int stepsPerFrame = 10;

        private List<int>[,] domains;
        private bool[,,] compatible;
        private readonly Queue<WFCArc> pendingArcs = new Queue<WFCArc>();
        private readonly Stack<WFCDecision> decisions = new Stack<WFCDecision>();

        private System.Random random;
        private bool firstObservation;
        private float nextAutomaticStep;

        private struct WFCArc
        {
            public int x, y, direction;

            public WFCArc(int x, int y, int direction)
            {
                this.x = x;
                this.y = y;
                this.direction = direction;
            }
        }

        private class WFCDecision
        {
            public Vector2Int cell;
            public List<int>[,] snapshot;
            public List<int> remaining;
        }

        public WFCTileSet TileSet => tileSet;
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int Connections => (int)neighborhood;
        public bool RandomSeed => randomSeed;
        public int Seed => seed;
        public int UsedSeed { get; private set; }
        public int Observations { get; private set; }
        public int PropagatedArcs { get; private set; }
        public int Backtracks { get; private set; }
        public int Contradictions { get; private set; }
        public int PendingArcs => pendingArcs.Count;
        public int Revision { get; private set; }
        public int TrainingVersion { get; private set; }
        public int GenerationVersion { get; private set; }
        public bool IsInitialized => domains != null;
        public bool Automatic { get; private set; }
        public bool Finished => Phase == WFCPhase.Completed || Phase == WFCPhase.Failed || Phase == WFCPhase.LimitReached;
        public WFCPhase Phase { get; private set; } = WFCPhase.NotInitialized;
        public Vector2Int LastObserved { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int LastChanged { get; private set; } = new Vector2Int(-1, -1);
        public string Status { get; private set; } = "Carga una matriz para comenzar.";
        public string TrainingText { get; private set; } = "";

        private void Start()
        {
            if (initializeOnStart && trainingMatrix != null && LoadTraining(trainingMatrix.text))
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!Automatic || Time.unscaledTime < nextAutomaticStep)
            {
                return;
            }

            for (int i = 0; i < stepsPerFrame && !Finished; i++)
            {
                Step();
            }

            nextAutomaticStep = Time.unscaledTime + automaticStepDelay;
        }

        // Control general
        public bool LoadTraining(string text)
        {
            try
            {
                tileSet.Learn(text);
            }
            catch (Exception ex)
            {
                Status = ex.Message;
                return false;
            }

            Clear();
            TrainingText = text;
            TrainingVersion++;
            Status = $"Aprendido: {tileSet.Width} x {tileSet.Height}. Probabilidades calculadas.";
            OutputObservations();
            return true;
        }

        public bool Configure(int newWidth, int newHeight, int connections, bool useRandomSeed, int fixedSeed)
        {
            if (newWidth < 1 || newHeight < 1 || newWidth > 128 || newHeight > 128 || (long)newWidth * newHeight > 4096)
            {
                Status = "Dimensiones invalidas. Maximo 4096 celdas.";
                return false;
            }

            if (connections != 4 && connections != 8)
            {
                Status = "El vecindario debe ser 4 u 8.";
                return false;
            }

            width = newWidth;
            height = newHeight;
            neighborhood = (WFCNeighborhood)connections;
            randomSeed = useRandomSeed;
            seed = fixedSeed;

            Clear();
            return true;
        }

        // El debugger reutiliza este mismo algoritmo para comparar dos vecindarios.
        // Se comparte el entrenamiento aprendido; cada instancia tiene sus dominios y RNG.
        public bool InitializeComparison(WFCGenerator source, int connections, int sharedSeed)
        {
            initializeOnStart = false;
            tileSet = source.tileSet;
            TrainingText = source.TrainingText;
            TrainingVersion = source.TrainingVersion;
            cellSize = source.cellSize;
            maxBacktracks = source.maxBacktracks;
            automaticStepDelay = source.automaticStepDelay;
            stepsPerFrame = source.stepsPerFrame;
            return Configure(source.Width, source.Height, connections, false, sharedSeed) && Initialize();
        }

        // 1. Inicializar dominios
        public bool Initialize()
        {
            Clear();

            if (!tileSet.HasSample)
            {
                Status = "Primero carga una matriz de entrenamiento.";
                return false;
            }

            if (width < 1 || height < 1 || (long)width * height > 4096 || (Connections != 4 && Connections != 8))
            {
                Status = "Dimensiones o vecindario invalidos.";
                return false;
            }

            int nextSeed = randomSeed ? Guid.NewGuid().GetHashCode() : seed;

            if (randomSeed && nextSeed == UsedSeed)
            {
                nextSeed = unchecked(nextSeed + 1);
            }

            UsedSeed = nextSeed;
            random = new System.Random(UsedSeed);
            firstObservation = true;

            Observations = 0;
            PropagatedArcs = 0;
            Backtracks = 0;
            Contradictions = 0;
            LastObserved = LastChanged = new Vector2Int(-1, -1);

            if (!PatternsFromSample())
            {
                return false;
            }

            BuildPropagator();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    EnqueueOutgoing(x, y);
                }
            }

            Phase = PendingArcs > 0 ? WFCPhase.Propagating : WFCPhase.ReadyToObserve;
            GenerationVersion++;
            Status = "Mapa preparado. Comprobando restricciones iniciales.";

            CheckCompletion();
            OutputObservations();
            return true;
        }

        private bool PatternsFromSample()
        {
            var available = new List<int>();

            for (int t = 0; t < tileSet.Tiles.Count; t++)
            {
                if (tileSet.Tiles[t].available && tileSet.Frequencies[t] > 0)
                {
                    available.Add(t);
                }
            }

            if (available.Count == 0)
            {
                Status = "No hay tiles observados y habilitados.";
                return false;
            }

            domains = new List<int>[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    domains[x, y] = new List<int>(available);
                }
            }

            return true;
        }

        // ========================================================
        // ETAPA 1: RESTRICCIONES APRENDIDAS
        // ========================================================
        /*
         * OBJETIVO:
         * Construir el indice de compatibilidad consultando
         * relaciones observadas en la matriz de entrenamiento.
         *
         * A diferencia del laboratorio anterior, las conexiones
         * no dependen de banderas manuales en los prefabs.
         * El vecindario elegido define que direcciones se aplican.
         *
         * Si una pareja dirigida no aparece en el entrenamiento,
         * no debe ser admitida por el propagador.
         */
        private void BuildPropagator()
        {
            int count = tileSet.Tiles.Count;
            compatible = new bool[Connections, count, count];

            /*
             * TODO 5 - CONSTRUIR COMPATIBILIDADES
             *
             * Completar el indice de compatibilidad a partir
             * de las relaciones aprendidas en WFCTileSet.
             *
             * CONSIDERAR:
             * - Solo se incluyen las direcciones del vecindario activo.
             * - El orden de la pareja y su direccion importan.
             * - Las reglas deben provenir del entrenamiento.
             */
        }

        // 3. Observacion
        public bool Observe()
        {
            if (Phase != WFCPhase.ReadyToObserve)
            {
                return false;
            }

            Vector2Int cell = firstObservation ? FindRandomUnresolvedCell() : FindLowestEntropy();

            if (cell.x < 0)
            {
                CheckCompletion();
                return false;
            }

            double[] weights = GetWeights(cell.x, cell.y);
            var remaining = new List<int>();

            foreach (int tile in domains[cell.x, cell.y])
            {
                if (weights[tile] > 0)
                {
                    remaining.Add(tile);
                }
            }

            int chosen = ChooseWeightedPattern(remaining, weights);
            remaining.Remove(chosen);

            decisions.Push(new WFCDecision
            {
                cell = cell,
                snapshot = CopyDomains(domains),
                remaining = remaining
            });

            firstObservation = false;
            Collapse(cell, chosen);
            return true;
        }

        private Vector2Int FindRandomUnresolvedCell()
        {
            int seen = 0;
            Vector2Int selected = new Vector2Int(-1, -1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (domains[x, y].Count > 1 && random.Next(++seen) == 0)
                    {
                        selected = new Vector2Int(x, y);
                    }
                }
            }

            return selected;
        }

        private Vector2Int FindLowestEntropy()
        {
            double best = double.PositiveInfinity;
            Vector2Int selected = new Vector2Int(-1, -1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (domains[x, y].Count <= 1)
                    {
                        continue;
                    }

                    double[] weights = GetWeights(x, y);
                    double entropy = CalculateEntropy(domains[x, y], weights);
                    entropy += random.NextDouble() * 1e-9;

                    if (entropy < best)
                    {
                        best = entropy;
                        selected = new Vector2Int(x, y);
                    }
                }
            }

            return selected;
        }

        // ========================================================
        // ETAPA 2: PESOS DE OBSERVACION
        // ========================================================
        /*
         * OBJETIVO:
         * Estimar la preferencia por los patrones restantes de
         * una celda usando la evidencia aprendida.
         *
         * Dos casos:
         * - Si aun no existe contexto resuelto, se usan las
         *   frecuencias globales del ejemplo.
         * - Con vecinos resueltos, se consideran las probabilidades
         *   condicionales asociadas a sus direcciones.
         *
         * EJEMPLO:
         * Cerca de una region de agua, el modelo puede favorecer
         * Agua o Arena segun los pares aprendidos y el vecindario.
         * No se garantiza recrear un lago de la misma forma.
         */
        public double[] GetWeights(int x, int y)
        {
            var weights = new double[tileSet.Tiles.Count];

            if (!IsInitialized || !IsInBounds(x, y))
            {
                return weights;
            }

            // Base temporal: pesos globales de los tiles aun permitidos.
            foreach (int tile in domains[x, y])
            {
                weights[tile] = tileSet.Global(tile);
            }

            /*
             * TODO 6 - PESOS CONDICIONALES
             *
             * Adaptar los pesos de los candidatos considerando
             * los vecinos que ya tienen un patron asignado.
             *
             * CONSIDERAR:
             * - Un vecino sin resolver no aporta el mismo contexto.
             * - Importan la direccion y el tile observado.
             * - La seleccion solo debe considerar el dominio actual.
             * - Varias observaciones vecinas deben combinarse
             *   siguiendo el criterio del laboratorio: promedio.
             *
             * CalculateEntropy() y ChooseWeightedPattern() ya
             * utilizan los valores que devuelve este metodo.
             */

            return weights;
        }

        // Entropia ponderada
        public static double CalculateEntropy(IReadOnlyList<int> domain, double[] weights)
        {
            double sum = 0;
            double weightedLog = 0;

            foreach (int tile in domain)
            {
                if (weights[tile] > 0)
                {
                    sum += weights[tile];
                    weightedLog += weights[tile] * Math.Log(weights[tile]);
                }
            }

            if (sum <= 0)
            {
                throw new InvalidOperationException("El dominio no tiene pesos positivos.");
            }

            return Math.Log(sum) - weightedLog / sum;
        }

        // Seleccion aleatoria ponderada
        private int ChooseWeightedPattern(IReadOnlyList<int> domain, double[] weights)
        {
            double total = 0;

            foreach (int tile in domain)
            {
                total += weights[tile];
            }

            if (total <= 0)
            {
                throw new InvalidOperationException("No existen candidatos con peso positivo.");
            }

            double roll = random.NextDouble() * total;
            int last = -1;

            foreach (int tile in domain)
            {
                if (weights[tile] <= 0)
                {
                    continue;
                }

                last = tile;
                roll -= weights[tile];

                if (roll < 0)
                {
                    return tile;
                }
            }

            return last;
        }

        private void Collapse(Vector2Int cell, int chosen)
        {
            domains[cell.x, cell.y].Clear();
            domains[cell.x, cell.y].Add(chosen);

            LastObserved = LastChanged = cell;
            Observations++;

            EnqueueOutgoing(cell.x, cell.y);
            Phase = PendingArcs > 0 ? WFCPhase.Propagating : WFCPhase.ReadyToObserve;
            Status = $"Observar ({cell.x},{cell.y}): {tileSet.Tiles[chosen].id}.";

            CheckCompletion();
        }

        // 4. Propagacion de restricciones
        public bool Propagate()
        {
            if (Phase != WFCPhase.Propagating)
            {
                return false;
            }

            while (pendingArcs.Count > 0)
            {
                WFCArc arc = pendingArcs.Dequeue();

                int nx = arc.x + WFCTileSet.Offsets[arc.direction].x;
                int ny = arc.y + WFCTileSet.Offsets[arc.direction].y;

                List<int> source = domains[arc.x, arc.y];
                List<int> target = domains[nx, ny];
                int before = target.Count;

                for (int i = target.Count - 1; i >= 0; i--)
                {
                    bool supported = false;

                    foreach (int origin in source)
                    {
                        if (compatible[arc.direction, origin, target[i]])
                        {
                            supported = true;
                            break;
                        }
                    }

                    if (!supported)
                    {
                        target.RemoveAt(i);
                    }
                }

                PropagatedArcs++;

                if (target.Count == before)
                {
                    continue;
                }

                LastChanged = new Vector2Int(nx, ny);

                if (target.Count == 0)
                {
                    ReportContradiction(nx, ny);
                    return true;
                }

                EnqueueOutgoing(nx, ny);
                Status = $"Propagar ({nx},{ny}): {before} -> {target.Count} candidatos.";
                return true;
            }

            Phase = WFCPhase.ReadyToObserve;
            Status = "Propagacion terminada. Siguiente: observar.";
            CheckCompletion();
            return true;
        }

        private void EnqueueOutgoing(int x, int y)
        {
            for (int d = 0; d < Connections; d++)
            {
                int nx = x + WFCTileSet.Offsets[d].x;
                int ny = y + WFCTileSet.Offsets[d].y;

                if (IsInBounds(nx, ny))
                {
                    pendingArcs.Enqueue(new WFCArc(x, y, d));
                }
            }
        }

        private void ReportContradiction(int x, int y)
        {
            Phase = WFCPhase.Contradiction;
            Contradictions++;
            Status = $"Contradiccion en ({x},{y}). Siguiente: backtracking.";
        }

        private void CheckCompletion()
        {
            if (!IsInitialized || Phase == WFCPhase.Contradiction || PendingArcs > 0)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (domains[x, y].Count != 1)
                    {
                        return;
                    }
                }
            }

            Phase = WFCPhase.Completed;
            Status = "Generacion completada.";
        }

        // ========================================================
        // ETAPA 3: BACKTRACKING
        // ========================================================
        /*
         * OBJETIVO:
         * Intentar recuperar una generacion cuando una decision
         * provoca un dominio vacio durante la propagacion.
         *
         * WFCDecision almacena la posicion observada, una copia
         * del estado anterior y las alternativas aun disponibles.
         * El historial de decisiones utiliza una pila (Stack).
         *
         * EJEMPLO:
         * Si una celda se resolvio con A y aparece una contradiccion,
         * puede ser necesario restaurar el estado anterior y
         * explorar otra alternativa de esa misma decision.
         *
         * El retroceso no consiste en vaciar y regenerar todo el mapa.
         */
        private List<int>[,] CopyDomains(List<int>[,] source)
        {
            var copy = new List<int>[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    copy[x, y] = new List<int>(source[x, y]);
                }
            }

            return copy;
        }

        private void Backtrack()
        {
            pendingArcs.Clear();

            /*
             * TODO 7 - RECUPERACION ANTE CONTRADICCIONES
             *
             * Recuperar una decision anterior que todavia ofrezca
             * alternativas y retomar la generacion desde ella.
             *
             * CONSIDERAR:
             * - La ultima decision no siempre tiene alternativas.
             * - Restaurar tambien los dominios alterados despues
             *   de esa decision.
             * - No volver a intentar indefinidamente el mismo tile.
             * - Respetar el limite maxBacktracks configurado.
             * - Informar cuando se agotan todas las alternativas.
             *
             * Las estructuras WFCDecision, decisions, CopyDomains()
             * y Collapse() se proporcionan como herramientas.
             */

            Phase = WFCPhase.Failed;
            Status = "Pendiente: implementar Backtrack().";
        }

        // 6. OutputObservations: notificar cambios al debugger
        private void OutputObservations()
        {
            Revision++;
        }

        public void Step()
        {
            switch (Phase)
            {
                case WFCPhase.ReadyToObserve:
                    Observe();
                    break;

                case WFCPhase.Propagating:
                    Propagate();
                    break;

                case WFCPhase.Contradiction:
                    Backtrack();
                    break;
            }

            if (Finished)
            {
                Automatic = false;
            }

            OutputObservations();
        }

        public void SetAutomatic(bool enabled)
        {
            Automatic = enabled && IsInitialized && !Finished;
            nextAutomaticStep = 0;
        }

        public void Clear()
        {
            Automatic = false;
            domains = null;
            compatible = null;
            pendingArcs.Clear();
            decisions.Clear();
            Phase = WFCPhase.NotInitialized;

            OutputObservations();
        }

        // Consultas utilizadas por WFCDebugger
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        public IReadOnlyList<int> GetDomain(int x, int y)
        {
            return IsInitialized && IsInBounds(x, y) ? domains[x, y] : null;
        }

        public int GetDomainCount(int x, int y)
        {
            return GetDomain(x, y)?.Count ?? -1;
        }

        public int GetResolvedTile(int x, int y)
        {
            return GetDomainCount(x, y) == 1 ? domains[x, y][0] : -1;
        }

        public Vector3 GetCellWorldPosition(int x, int y)
        {
            return transform.TransformPoint(new Vector3(x * cellSize, y * cellSize, 0));
        }

        public int[] GetResolvedCounts()
        {
            var counts = new int[tileSet.Tiles.Count];

            if (IsInitialized)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int tile = GetResolvedTile(x, y);

                        if (tile >= 0)
                        {
                            counts[tile]++;
                        }
                    }
                }
            }

            return counts;
        }

        public string MapCsv()
        {
            var csv = new StringBuilder();

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x > 0)
                    {
                        csv.Append(',');
                    }

                    int tile = GetResolvedTile(x, y);
                    csv.Append(tile < 0 ? "?" : tileSet.Tiles[tile].id);
                }

                csv.AppendLine();
            }

            return csv.ToString();
        }
    }
}
