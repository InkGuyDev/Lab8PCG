using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public enum WFCPhase
    {
        NotInitialized, ReadyToObserve, Propagating,
        Completed, Contradiction
    }

    public class WFCGenerator : MonoBehaviour
    {
        [Header("Tile Sets")]
        [SerializeField] private List<WFCTileSet> tileSets = new()
        {
            new WFCTileSet { id = "TileSet_01" },
            new WFCTileSet { id = "TileSet_02" }
        };
        [SerializeField, Min(0)] private int activeTileSetIndex = 0;

        [Header("Mapa")]
        [SerializeField, Min(1)] private int width = 16;
        [SerializeField, Min(1)] private int height = 12;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        [Header("Generacion")]
        [SerializeField] private bool randomSeed = false;
        [SerializeField] private int seed = 12345;

        [Header("Visualizacion")]
        [SerializeField] private Transform gridParent;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Text textStep;

        [Header("Interfaz")]
        [SerializeField] private InputField inputFieldSeed;
        [SerializeField] private Toggle toogleTLS1;
        [SerializeField] private Toggle toogleTLS2;
        [SerializeField] private InputField inputFieldWidth;
        [SerializeField] private InputField inputFieldHeight;

        private SpriteRenderer[,] tileRenderer;

        // --------------------------------------------------------
        // ESTRUCTURAS DEL ALGORITMO
        // --------------------------------------------------------

        /*
         * domains[x,y]:
         * Contiene los indices de los patrones que todavia pueden
         * ocupar la posicion (x,y).
         *
         * compatible[d,a,b]:
         * Indica si el patron B puede ubicarse en la direccion d
         * respecto del patron A.
         *
         * pendingArcs:
         * Relaciones entre celdas pendientes de comprobar
         * durante la propagacion.
         */

        private List<int>[,] domains;
        private bool[,,] compatible;
        private Queue<WFCArc> pendingArcs = new();
        private System.Random random;
        private bool firstObservation;

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

        private static readonly Vector2Int[] Offsets =
        {
            new Vector2Int(0, 1),   // North
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, -1),  // South
            new Vector2Int(-1, 0)   // West
        };

        public WFCPhase Phase { get; private set; } = WFCPhase.NotInitialized;
        public string Status { get; private set; } = "Sin inicializar.";

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int ActiveTileSetIndex => activeTileSetIndex;
        public bool IsInitialized => domains != null;

        public WFCTileSet ActiveTileSet =>
            tileSets != null && activeTileSetIndex >= 0 &&
            activeTileSetIndex < tileSets.Count
                ? tileSets[activeTileSetIndex] : null;

        // ========================================================
        // CONTROL GENERAL
        // ========================================================

        /*
         * El algoritmo WFC desarrolla cuatro tareas principales:
         *
         * 1. Preparar patrones.
         * 2. Construir restricciones.
         * 3. Generar mediante observacion y propagacion.
         * 4. Representar el resultado.
         *
         * Durante la generacion se trabaja con asignaciones
         * parciales. Los dominios disminuyen progresivamente
         * hasta que cada celda tenga un unico patron.
         *
         * Referencia: Karth y Smith (2017), Seccion 4.
         */

        public void SelectTileSet(int index)
        {
            if (tileSets == null || index < 0 || index >= tileSets.Count)
                return;

            activeTileSetIndex = index;
            Clear();
        }

        public bool Initialize()
        {
            Clear();

            SetGenerationsVariables();

            if (ActiveTileSet == null || ActiveTileSet.Tiles.Count == 0)
            {
                Status = "El TileSet seleccionado no contiene patrones.";
                Debug.LogWarning(Status, this);
                return false;
            }

            random = new System.Random(
                randomSeed ? Environment.TickCount : seed);

            firstObservation = true;

            // Preparar los dominios iniciales.
            if (!PatternsFromSample() || domains == null)
            {
                Status = "Pendiente: PatternsFromSample().";
                return false;
            }

            // Construir las relaciones de compatibilidad.
            BuildPropagator();

            Phase = WFCPhase.ReadyToObserve;
            Status = "Inicializado. Listo para observar.";

            OutputObservations();
            return true;
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
            }

            OutputObservations();
        }

        public void Clear()
        {
            if (tileRenderer != null)
            {
                foreach (var sr in tileRenderer)
                {
                    if (sr != null && sr.gameObject != null)
                    {
                        Destroy(sr.gameObject);
                    }
                }
                tileRenderer = null;
            }

            domains = null;
            compatible = null;
            pendingArcs.Clear();

            firstObservation = true;
            Phase = WFCPhase.NotInitialized;
            Status = "Sin inicializar.";
        }

        // ========================================================
        // ETAPA 1: PATTERNS FROM SAMPLE
        // ========================================================

        /*
         * OBJETIVO:
         * Preparar los patrones disponibles y los dominios
         * iniciales de todas las posiciones de la matriz.
         *
         * En esta version utilizamos Simple Tiled WFC:
         * los patrones corresponden a piezas definidas en
         * el TileSet, no a subimagenes extraidas automaticamente.
         *
         * EJEMPLO:
         *
         * Si el TileSet contiene cuatro patrones:
         *
         *   P0, P1, P2, P3
         *
         * Pero P2 esta deshabilitado, el dominio inicial es:
         *
         *   D(x,y) = {P0, P1, P3}
         *
         * Todas las celdas comienzan con esos patrones
         * disponibles. Durante WFC se iran descartando opciones.
         */

        private bool PatternsFromSample()
        {
            /*
             * TODO 1:
             *
             * 1. Identificar los patrones habilitados del TileSet.
             * 2. Crear los dominios de la matriz.
             * 3. Inicializar cada dominio con los indices
             *    de los patrones disponibles.
             *
             * Considerar:
             * - Cada celda mantiene su propio dominio.
             * - No deben incluirse patrones deshabilitados.
             * - Los dominios pueden tener distinta cantidad
             *   de elementos durante la generacion.
             *
             * Retornar true cuando la inicializacion sea valida.
             */

            // Lista del TileSet
            WFCTileSet tileSet = ActiveTileSet;
            if (tileSet == null) return false;

            // Patrones habilitados
            List<int> availableIndices = new List<int>();
            for (int i = 0; i < tileSet.Tiles.Count ; i++)
            {
                if (tileSet.Tiles[i] != null && tileSet.Tiles[i].available)
                {
                    availableIndices.Add(i);
                }
            }

            // Se indica el status
            if (availableIndices.Count == 0)
            {
                Status = "No hay patrones habilitados en el TileSet";
            }

            // Dominio de la matriz
            domains = new List<int>[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Dominio con los indices disponibles
                    domains[x, y] = new List<int>(availableIndices);
                }
            }

            return true;
            //return false;
        }

        // ========================================================
        // ETAPA 2: BUILD PROPAGATOR
        // ========================================================

        /*
         * OBJETIVO:
         * Construir un indice con las relaciones de compatibilidad
         * entre todos los patrones disponibles.
         *
         * La compatibilidad depende de la direccion relativa.
         *
         * EJEMPLO:
         *
         * Un camino horizontal tiene:
         *
         *   North = 0, East = 1, South = 0, West = 1
         *
         * Si queremos colocar otra pieza al Este, esta debe
         * tener una conexion compatible hacia el Oeste.
         *
         * Esto permite descartar combinaciones incorrectas
         * sin evaluar nuevamente sus conexiones durante
         * cada propagacion.
         *
         * compatible[d,a,b] almacena esta relacion.
         */

        private void BuildPropagator()
        {
            int count = ActiveTileSet.Tiles.Count;
            compatible = new bool[4, count, count];

            /*
             * TODO 2:
             *
             * Construir las relaciones entre pares de patrones
             * para las cuatro direcciones.
             *
             * Utilizar IsCompatible() definido en WFCTile.
             *
             * El arreglo "compatible" debe permitir consultar
             * posteriormente si un patron puede ser vecino
             * de otro en una direccion determinada.
             */

            // Recorrido por 'compatible'
            for (int d = 0; d < 4; d++)
            {
                WFCDirection direction = (WFCDirection)d; // Dir = 4

                for (int a = 0; a < count; a++)
                {
                    WFCTile tileA = ActiveTileSet.Tiles[a]; // Primer tile

                    for (int b = 0; b < count; b++)
                    {
                        WFCTile tileB = ActiveTileSet.Tiles[b]; // Tile vecino

                        // Verificacion si el tile es null 
                        if (tileA == null || tileB == null)
                        {
                            compatible[d, a, b] = false;
                            continue;
                        }

                        //Comparacion entre tileA y tileB segun la dirección
                        compatible[d, a, b] = tileA.IsCompatible(tileB, direction);
                    }
                }
            }
        }

        // ========================================================
        // ETAPA 3A: OBSERVE
        // ========================================================

        /*
         * OBJETIVO:
         * Seleccionar una celda que todavia no este resuelta
         * y asignarle uno de sus patrones posibles.
         *
         * EJEMPLO:
         *
         * Antes de observar:
         *
         *   D(2,3) = {P0, P2, P5, P7}
         *
         * Si se selecciona P5:
         *
         *   D(2,3) = {P5}
         *
         * La celda queda resuelta, pero sus vecinos pueden
         * mantener patrones incompatibles con esta decision.
         *
         * Por lo tanto, debe ejecutarse la propagacion.
         */

        public bool Observe()
        {
            if (Phase != WFCPhase.ReadyToObserve)
                return false;
            
            /*for (int i = 0; i < ActiveTileSet.Tiles.Count; i++)
            {
                if (ActiveTileSet.Tiles[i] == null)
                {
                    return Phase == WFCPhase.Contradiction;
                }
            }*/

            Vector2Int UnresolvedCell = firstObservation ? FindRandomUnresolvedCell() : FindLowestEntropy();
            firstObservation = false;

            if (UnresolvedCell.x == -1 || UnresolvedCell.y == -1)
            {
                CheckCompletion();
                return Phase == WFCPhase.Completed;
            }
            /*
             * TODO 3:
             *
             * 1. Seleccionar una celda no resuelta.
             * 2. Elegir uno de sus patrones disponibles.
             * 3. Mantener solamente el patron seleccionado.
             * 4. Preparar las relaciones que requieren
             *    propagacion.
             *
             * Considerar:
             * - Primera observacion: posicion aleatoria.
             * - Observaciones siguientes: menor entropia.
             * - La eleccion del patron considera su peso.
             *
             * Actualizar Phase y Status segun corresponda.
             */

            List<int> domain = domains[UnresolvedCell.x, UnresolvedCell.y];
            if (domain == null || domain.Count == 0)
            {
                ReportContradiction(UnresolvedCell.x, UnresolvedCell.y);
                return false;
            }

            int SellectedPatternIndex =  ChooseWeightedPattern(domain);

            domain.Clear();
            domain.Add(SellectedPatternIndex);
            EnqueueOutgoing(UnresolvedCell.x, UnresolvedCell.y);

            Phase = WFCPhase.Propagating;
            //Status = "Pendiente: Observe().";

            Status = "Observación realizada. Propagando...";

            return true;
        }

        // --------------------------------------------------------
        // SELECCION DE CELDA
        // --------------------------------------------------------

        private Vector2Int FindRandomUnresolvedCell()
        {
            /*
             * TODO:
             *
             * Seleccionar una posicion aleatoria entre aquellas
             * que todavia tengan mas de un patron disponible.
             *
             * No considerar posiciones ya resueltas.
             */

            List<Vector2Int> UnresolvedCells = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++){
                    if (domains[x,y].Count > 1)
                    {
                        UnresolvedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
            if (UnresolvedCells.Count == 0)
            {
                return new Vector2Int(-1, -1);
            }

            return UnresolvedCells[random.Next(UnresolvedCells.Count)];
            
        }

        private Vector2Int FindLowestEntropy()
        {
            /*
             * TODO:
             *
             * Seleccionar la posicion no resuelta con
             * menor entropia positiva.
             *
             * EJEMPLO SIMPLIFICADO (pesos iguales):
             *
             * Celda A: {P0, P1, P2, P3} -> 4 posibilidades
             * Celda B: {P0, P2}         -> 2 posibilidades
             *
             * B esta mas restringida y tiene prioridad.
             *
             * Cuando los pesos son diferentes, la entropia
             * debe considerar su distribucion.
             *
             * Se puede utilizar aleatoriedad para resolver
             * empates entre celdas igualmente restringidas.
             */


            float minEntropy = float.MaxValue;
            List <Vector2Int> candidates = new List<Vector2Int>();

            for (int x = 0;x <width ;x++ ) {
                for (int y = 0; y< height; y++) {
                    int count = GetDomainCount(x,y);
                    if (count <= 1) continue;
                    
                    float entropy = (float)(count + (float)random.NextDouble() * 0.01);

                    if (entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        candidates.Clear();
                        candidates.Add(new Vector2Int(x, y));
                    }
                    else if (Mathf.Approximately(entropy, minEntropy))
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return new Vector2Int(-1, -1);
            }

            return candidates[random.Next(candidates.Count)];
        }

        // --------------------------------------------------------
        // SELECCION DE PATRON
        // --------------------------------------------------------

        private int ChooseWeightedPattern(IReadOnlyList<int> domain)
        {
            /*
             * TODO:
             *
             * Seleccionar aleatoriamente un patron del dominio
             * considerando su peso.
             *
             * EJEMPLO:
             *
             * Patron A -> peso 3
             * Patron B -> peso 1
             *
             * A tiene mayor probabilidad de seleccion,
             * pero B sigue siendo una alternativa valida.
             *
             * La seleccion debe limitarse a los patrones
             * que permanecen en el dominio.
             */



            float totalWheight = 0f;

            for (int i = 0; i < domain.Count; i++)
            {
                int tileIndex = domain[i];
                totalWheight += ActiveTileSet.Tiles[tileIndex].weight;
            }

            float randomValue = (float)(random.NextDouble() * totalWheight);
            float currentSum = 0f;

            for (int i = 0; i < domain.Count; i++)
            {
                int tileIndex = domain[i];
                currentSum += ActiveTileSet.Tiles[tileIndex].weight;
                if (randomValue<= currentSum)
                {
                    return tileIndex;
                }
            }

            return domain[domain.Count-1];
        }

        // ========================================================
        // ETAPA 3B: PROPAGATE
        // ========================================================

        /*
         * OBJETIVO:
         * Actualizar los dominios de las celdas relacionadas
         * despues de una observacion o de otra reduccion.
         *
         * EJEMPLO:
         *
         * Se observa un camino horizontal en la celda A.
         * Su conexion Este esta abierta.
         *
         * La celda B, ubicada al Este, tiene:
         *
         *   D(B) = {Horizontal, Vertical}
         *
         * Horizontal tiene West = 1.
         * Vertical tiene West = 0.
         *
         * Por lo tanto, Vertical deja de ser valido:
         *
         *   D(B) = {Horizontal}
         *
         * La reduccion de B puede afectar a sus propios
         * vecinos. Este proceso continua hasta que no
         * queden restricciones pendientes.
         */

        public bool Propagate()
        {
            if (Phase != WFCPhase.Propagating)
                return false;

            /*
             * TODO 4:
             *
             * Mientras existan relaciones pendientes:
             *
             * 1. Revisar los dominios de las celdas relacionadas.
             * 2. Identificar patrones sin una alternativa
             *    compatible en la celda vecina.
             * 3. Eliminar los patrones incompatibles.
             * 4. Continuar propagando cuando existan cambios.
             *
             * CONSIDERACIONES:
             *
             * - Un dominio solamente puede mantener o reducir
             *   su cantidad de patrones durante la generacion.
             *
             * - Si queda un unico patron, la celda esta resuelta.
             *
             * - Si el dominio queda vacio, existe una contradiccion.
             *
             * - Cuando no quedan relaciones pendientes, puede
             *   continuar la siguiente observacion.
             *
             * No se requiere implementar backtracking.
             */
            while (pendingArcs.Count > 0)
            {
                WFCArc arc = pendingArcs.Dequeue();
                int x = arc.x;
                int y = arc.y;
                int dir = arc.direction;

                int nx = x + Offsets[dir].x;
                int ny = y + Offsets[dir].y;

                if (!IsInBounds(nx, ny))
                    continue;

                List<int> originDomain = domains[x, y];
                List<int> neighborDomain = domains[nx, ny];

                if (originDomain == null || neighborDomain == null)
                {
                    continue;
                }
                bool domainChanged = false;

                for (int i = neighborDomain.Count - 1; i >= 0; i--)
                {
                    int neighborPattern = neighborDomain[i];
                    bool hasCompatibleOption = false;
                    for (int j = 0; j < originDomain.Count; j++)
                    {
                        int originPattern = originDomain[j];
                        if (compatible[dir, originPattern, neighborPattern])
                        {
                            hasCompatibleOption = true;
                            break;
                        }
                    }
                    if (!hasCompatibleOption)
                    {
                        neighborDomain.RemoveAt(i);
                        domainChanged = true;
                    }
                }
                if (neighborDomain.Count == 0)
                {
                    ReportContradiction(nx, ny);
                    return false;
                }
                if (domainChanged)
                {
                    EnqueueOutgoing(nx, ny);
                }
            }

            CheckCompletion();

            if (Phase != WFCPhase.Completed && Phase != WFCPhase.Contradiction)
            {
                Phase = WFCPhase.ReadyToObserve;
                Status = "Propagación finalizada. Listo para la siguiente observación.";
            }
            return true;
        }

        // --------------------------------------------------------
        // HERRAMIENTAS DE PROPAGACION
        // --------------------------------------------------------

        /*
         * WFCArc representa una relacion dirigida:
         *
         *   (x,y) ---- direccion ----> vecino
         *
         * EnqueueOutgoing agrega las relaciones entre una
         * celda y sus vecinos existentes.
         *
         * La cola pendingArcs permite revisar estas relaciones
         * durante la propagacion.
         *
         * Este metodo se entrega implementado.
         */

        private void EnqueueOutgoing(int x, int y)
        {
            for (int d = 0; d < 4; d++)
            {
                int nx = x + Offsets[d].x;
                int ny = y + Offsets[d].y;

                if (IsInBounds(nx, ny))
                    pendingArcs.Enqueue(new WFCArc(x, y, d));
            }
        }

        // Se utiliza cuando alguna celda pierde todas
        // sus posibilidades de asignacion.
        private void ReportContradiction(int x, int y)
        {
            Phase = WFCPhase.Contradiction;
            Status = $"Contradiccion en ({x},{y}): dominio vacio.";
            Debug.LogWarning(Status, this);
        }

        // Comprueba si todas las posiciones tienen un unico
        // patron y ya no existen relaciones pendientes.
        private void CheckCompletion()
        {
            if (domains == null || Phase == WFCPhase.Contradiction ||
                pendingArcs.Count > 0)
                return;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (domains[x, y] == null || domains[x, y].Count != 1)
                        return;

            Phase = WFCPhase.Completed;
            Status = "Generacion completada.";
        }

        // ========================================================
        // ETAPA 4: OUTPUT OBSERVATIONS
        // ========================================================

        /*
         * OBJETIVO:
         * Representar el estado actual de la generacion.
         *
         * Durante WFC, una celda puede encontrarse:
         *
         * - Sin resolver: mantiene multiples patrones.
         * - Resuelta: tiene un unico patron.
         * - En contradiccion: no tiene patrones disponibles.
         *
         * EJEMPLO:
         *
         *   8  4  2
         *   4  1  2
         *   8  4  8
         *
         * Los numeros representan las posibilidades restantes
         * por celda. La posicion central ya esta resuelta.
         *
         * La visualizacion deberia permitir observar como
         * los dominios cambian durante la generacion.
         *
         * Puede utilizarse SpriteRenderer, Gizmos, Canvas
         * u otros elementos de Unity.
         */

        private void OutputObservations()
        {
            /*
             * TODO 5:
             *
             * Desarrollar una visualizacion propia.
             *
             * Representar los patrones asignados y las
             * posiciones que todavia no estan resueltas.
             *
             * Debe ser posible identificar los cambios
             * producidos por Observe() y Propagate().
             *
             * La representacion se puede implementar aqui
             * o delegar a componentes creados por el grupo.
             *
             * Evitar recrear innecesariamente todos los
             * objetos graficos en cada actualizacion.
             */

            if (!IsInitialized || domains == null) return;

            if (tileRenderer == null || tileRenderer.GetLength(0) != width || tileRenderer.GetLength(1) != height)
            {
                InitializeGridVisuals();
            }

            WFCTileSet currentTileSet = ActiveTileSet; 

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SpriteRenderer sr = tileRenderer[x, y]; 
                    if (sr == null) continue;

                    int domainCount = GetDomainCount(x, y);

                    if (domainCount == 1)
                    {
                        int selectedTileIndex = domains[x, y][0];
                        WFCTile tile = currentTileSet.Tiles[selectedTileIndex];

                        sr.sprite = tile.sprite;
                        sr.color = Color.white;
                    }
                    else if (domainCount > 1)
                    {
                        // CASO B: Celda no resuelta (superposición) ->
                        // Opcion simple: Mostrar el Sprite del primer patrón candidato pero semitransparente
                        int sampleTileIndex = domains[x, y][0];
                        WFCTile sampleTile = currentTileSet.Tiles[sampleTileIndex];

                        sr.sprite = sampleTile.sprite;
                        // Ajustamos el Alpha (A) para indicar que aún no está resuelta
                        sr.color = new Color(1f, 1f, 1f, 0.25f);
                    }
                    else if (domainCount == 0)
                    {
                        // CASO C: Contradicción -> Mostrar en rojo o transparente sin sprite
                        sr.sprite = null;
                    }
                }
            }
        }

        private void InitializeGridVisuals()
        {
            // Limpiar objetos visuales anteriores si existen
            if (tileRenderer != null)
            {
                foreach (var sr in tileRenderer)
                {
                    if (sr != null) Destroy(sr.gameObject);
                }
            }

            tileRenderer = new SpriteRenderer[width, height];

            if (gridParent == null)
            {
                GameObject parentObj = new GameObject("WFC_Grid_Visuals");
                gridParent = parentObj.transform;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = GetCellWorldPosition(x, y);
                    GameObject cellObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, gridParent);
                    cellObj.name = $"Cell_({x},{y})";

                    // Ajustar el tamaño del GameObject según cellSize
                    cellObj.transform.localScale = Vector3.one * cellSize;

                    SpriteRenderer sr = cellObj.GetComponent<SpriteRenderer>();
                    tileRenderer[x, y] = sr;
                }
            }
        }

        // ========================================================
        // CONSULTAS PARA LA VISUALIZACION
        // ========================================================

        // Verifica si una posicion pertenece a la matriz.
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        // Obtiene la cantidad de patrones posibles.
        // Devuelve -1 para una posicion invalida.
        public int GetDomainCount(int x, int y)
        {
            if (!IsInitialized || !IsInBounds(x, y))
                return -1;

            return domains[x, y] != null
                ? domains[x, y].Count : -1;
        }

        // Permite consultar los patrones restantes.
        // Los valores corresponden a indices del TileSet.
        public IReadOnlyList<int> GetDomain(int x, int y)
        {
            if (!IsInitialized || !IsInBounds(x, y))
                return null;

            return domains[x, y];
        }

        // Obtiene el centro de una celda en coordenadas
        // globales, considerando las dimensiones de la matriz.
        public Vector3 GetCellWorldPosition(int x, int y)
        {
            Vector3 position = new Vector3(
                (x - (width - 1) * 0.5f) * cellSize,
                (y - (height - 1) * 0.5f) * cellSize,
                0f);

            return transform.TransformPoint(position);
        }


        /*private void OnDrawGizmos()
        {
            if (!IsInitialized || domains == null) return;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 pos = GetCellWorldPosition(x, y);
                    int count = GetDomainCount(x, y);

                    if (count == 1)
                    {
                        // Celda resuelta -> Verde
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(pos, Vector3.one * cellSize * 0.9f);
                    }
                    else if (count > 1)
                    {
                        // Celda con múltiples opciones -> Blanco/Transparente
                        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                        Gizmos.DrawWireCube(pos, Vector3.one * cellSize * 0.9f);
                    }
                    else if (count == 0)
                    {
                        // Contradicción -> Rojo
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(pos, Vector3.one * cellSize * 0.9f);
                    }
                }
            }
        }*/


        public void OnButtonInitialize()
        {
            Initialize();
        }

        public void OnButtonNextStep()
        {
            /*if (Phase == WFCPhase.ReadyToObserve || Phase == WFCPhase.Propagating)
            {
                Step();
            }*/

            if (Phase == WFCPhase.ReadyToObserve)
            {
                Step();
                textStep.text = "Paso: Observando";
            }
            else if (Phase == WFCPhase.Propagating)
            {
                Step();
                textStep.text = "Paso: Propagando";
            }
        }

        public void OnButtonGenerateAll()
        {
            if (!Initialize())
            {
                return;
            }

            while (Phase == WFCPhase.Propagating || Phase == WFCPhase.ReadyToObserve)
            {
                Step();
            }

            Debug.Log($"[WFC] Usando TileSet: {ActiveTileSet.id} con {ActiveTileSet.Tiles.Count} tiles.");
        }

        public void SetGenerationsVariables()
        {
            // Seed
            if (string.IsNullOrEmpty(inputFieldSeed.text))
            {
                inputFieldSeed.text = seed.ToString();
            }
            else
            {
                if (int.TryParse(inputFieldSeed.text, out int newSeed))
                {
                    seed = newSeed;
                }
            }

            // TileSet seleccionado
            switch (ActiveTileSetIndex)
            {
                case 0: toogleTLS1.isOn = true; toogleTLS2.isOn = false; break;
                case 1: toogleTLS2.isOn = true; toogleTLS1.isOn = false; break;
            }

            // Grid dimensiones
            // width
            if (string.IsNullOrEmpty(inputFieldWidth.text))
            {
                inputFieldWidth.text = width.ToString();
            }
            else
            {
                if (int.TryParse(inputFieldWidth.text, out int newWidth))
                {
                    width = newWidth;
                }
            }
            // height
            if (string.IsNullOrEmpty(inputFieldHeight.text))
            {
                inputFieldHeight.text = height.ToString();
            }
            else
            {
                if (int.TryParse(inputFieldHeight.text, out int newHeight))
                {
                    height = newHeight;
                }
            }
        }

        private void Update()
        {
            if (toogleTLS1.isOn == true)
            {
                toogleTLS2.isOn = false;
                activeTileSetIndex = 0;
            }
            if (toogleTLS2.isOn == true)
            {
                toogleTLS1.isOn = false;
                activeTileSetIndex = 1;
            }
        }
    }
}
