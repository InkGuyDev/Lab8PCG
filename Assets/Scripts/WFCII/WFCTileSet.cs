
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Laboratorios.Lab10.WaveFunctionCollapse
{
    /*
     * ========================================================
     * LABORATORIO 8 - WFC II: APRENDIZAJE DESDE UNA MATRIZ
     * ========================================================
     *
     * OBJETIVO:
     * Aprender que patrones aparecen en un ejemplo, con que
     * frecuencia y que relaciones mantienen con sus vecinos.
     *
     * La entrada es un TXT/CSV de simbolos de terreno:
     *
     *   P,P,P,R,R
     *   P,P,R,A,R
     *   B,P,P,R,R
     *
     * MODELO DE DATOS:
     *   Sample[x,y]     -> indice del tile en esa posicion.
     *   Frequencies[t] -> apariciones globales del tile t.
     *   Counts[d,a,b]  -> veces que b aparece en direccion d de a.
     *   Weights[d,a,b] -> probabilidad condicional aprendida.
     *
     * DIRECCIONES:
     *   Cardinal: N, E, S, W.
     *   Moore:    anteriores + NE, SE, SW, NW.
     *
     * Las direcciones tienen un orden compartido con WFCGenerator.
     * Se entrega resuelta la estructura de datos, el flujo Learn(),
     * las consultas de estadisticas y las validaciones de entrada.
     * Los bloques TODO contienen las etapas a desarrollar.
     */

    [Serializable]
    public class WFCTileSet
    {
        public string id = "Terreno";

        [SerializeField]
        private List<WFCTile> tiles = new List<WFCTile>
        {
            new WFCTile { id = "P" },
            new WFCTile { id = "R" },
            new WFCTile { id = "A" },
            new WFCTile { id = "B" }
        };

        public IReadOnlyList<WFCTile> Tiles => tiles;
        public int[,] Sample { get; private set; }
        public int[] Frequencies { get; private set; }
        public int[,,] Counts { get; private set; }
        public double[,,] Weights { get; private set; }

        public int Width => Sample == null ? 0 : Sample.GetLength(0);
        public int Height => Sample == null ? 0 : Sample.GetLength(1);
        public bool HasSample => Sample != null;

        public static readonly Vector2Int[] Offsets =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1)
        };

        public static readonly string[] Directions = { "N", "E", "S", "W", "NE", "SE", "SW", "NW" };

        public static int Opposite(int direction)
        {
            return direction < 4 ? (direction + 2) % 4 : 4 + (direction - 4 + 2) % 4;
        }

        public void Learn(string text)
        {
            int[,] matrix = LoadMatrix(text);
            int[] frequencies = CountFrequencies(matrix);
            int[,,] counts = CountNeighbors(matrix);
            double[,,] probabilities = CalculateProbabilities(counts);

            Sample = matrix;
            Frequencies = frequencies;
            Counts = counts;
            Weights = probabilities;
        }

        // ========================================================
        // ETAPA 1: CARGAR LA MATRIZ DE ENTRENAMIENTO
        // ========================================================
        /*
         * OBJETIVO:
         * Interpretar el ejemplo textual como una matriz de indices.
         *
         * Las filas del TXT/CSV se leen desde arriba hacia abajo;
         * en Unity, la coordenada Y del mapa crece hacia arriba.
         * La conversion debe respetar ese cambio de orientacion.
         *
         * Se proporciona el lector de filas, la validacion del
         * formato y la asociacion simbolo-indice.
         */
        private int[,] LoadMatrix(string text)
        {
            var symbols = new Dictionary<string, int>();

            for (int t = 0; t < tiles.Count; t++)
            {
                string symbol = tiles[t]?.id;

                if (string.IsNullOrEmpty(symbol) || symbol.Length != 1 || symbols.ContainsKey(symbol))
                {
                    throw new FormatException("Cada tile requiere un ID unico de un caracter.");
                }

                symbols.Add(symbol, t);
            }

            if (symbols.Count == 0)
            {
                throw new FormatException("No hay tiles configurados.");
            }

            var rows = new List<string[]>();
            string[] lines = (text ?? "").TrimStart('\uFEFF').Replace("\r", "").Split('\n');

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                string[] tokens;

                if (line.Contains(",") || line.Contains(";"))
                {
                    tokens = line.Split(new[] { ',', ';' });
                }
                else if (line.IndexOfAny(new[] { ' ', '\t' }) >= 0)
                {
                    tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    tokens = SplitCharacters(line);
                }

                if (tokens.Length == 0 || (rows.Count > 0 && tokens.Length != rows[0].Length))
                {
                    throw new FormatException("La matriz debe ser rectangular.");
                }

                for (int x = 0; x < tokens.Length; x++)
                {
                    tokens[x] = tokens[x].Trim();

                    if (!symbols.ContainsKey(tokens[x]))
                    {
                        throw new FormatException("Tile desconocido: " + tokens[x]);
                    }
                }

                rows.Add(tokens);
            }

            if (rows.Count == 0 || rows[0].Length == 0)
            {
                throw new FormatException("La matriz esta vacia.");
            }

            if ((long)rows.Count * rows[0].Length > 4096)
            {
                throw new FormatException("Maximo 4096 celdas.");
            }

            int width = rows[0].Length;
            int height = rows.Count;
            var matrix = new int[width, height];

            /*
             * TODO 1 - INTERPRETACION DE LA MATRIZ
             *
             * Completar la conversion de las filas validadas a
             * posiciones (x,y) de la matriz de entrenamiento.
             *
             * CONSIDERAR:
             * - Cada simbolo debe corresponder a un indice de tiles.
             * - La disposicion espacial debe conservar el ejemplo.
             * - La orientacion de las filas debe coincidir con Unity.
             *
             * La matriz devuelta debe tener las dimensiones leidas.
             */

            return matrix;
        }

        // ========================================================
        // ETAPA 2: FRECUENCIAS GLOBALES
        // ========================================================
        /*
         * OBJETIVO:
         * Contar la presencia de cada tipo de terreno en Sample.
         *
         * EJEMPLO:
         * Una muestra con 12 celdas puede contener 5 P, 4 R,
         * 2 A y 1 B. El conteo es diferente de la probabilidad:
         * esta ultima se obtiene considerando el total de celdas.
         */
        private int[] CountFrequencies(int[,] matrix)
        {
            var frequencies = new int[tiles.Count];

            /*
             * TODO 2 - FRECUENCIAS GLOBALES
             *
             * Obtener el numero de apariciones de cada tile a
             * partir de la matriz de entrenamiento.
             *
             * CONSIDERAR:
             * - No asumir un tamaño fijo ni una distribucion uniforme.
             * - Cada posicion aporta una aparicion a un solo tile.
             * - Global() ya transforma estos conteos a probabilidades.
             */

            return frequencies;
        }

        // ========================================================
        // ETAPA 3: APRENDIZAJE DE RELACIONES LOCALES
        // ========================================================
        /*
         * OBJETIVO:
         * Reconocer que tiles pueden aparecer en cada direccion
         * respecto de un tile de origen.
         *
         * EJEMPLO CONCEPTUAL:
         * Un lago puede contener muchos pares Agua-Agua, mientras
         * su borde presenta pares Agua-Arena. El orden y la
         * direccion de la pareja importan.
         *
         * Primero analizar las cuatro direcciones cardinales.
         * Despues extender el aprendizaje a las cuatro diagonales.
         * Evitar interpretar celdas exteriores como vecinos reales.
         */
        private int[,,] CountNeighbors(int[,] matrix)
        {
            int width = matrix.GetLength(0);
            int height = matrix.GetLength(1);
            var counts = new int[8, tiles.Count, tiles.Count];

            /*
             * TODO 3 - CONTEOS DE VECINDAD (4 Y 8 CONEXIONES)
             *
             * Registrar las relaciones observadas entre cada
             * tile y sus vecinos de la matriz de entrenamiento.
             *
             * CONSIDERAR:
             * - Los desplazamientos disponibles estan en Offsets.
             * - El conteo depende de direccion, origen y vecino.
             * - En los bordes hay menos vecinos validos.
             * - Las diagonales deben aprenderse por separado.
             *
             * Los conteos iguales a cero representan relaciones
             * que no se observaron en esa muestra.
             */

            return counts;
        }

        // ========================================================
        // ETAPA 4: PROBABILIDADES CONDICIONALES
        // ========================================================
        /*
         * OBJETIVO:
         * Transformar las frecuencias de las relaciones aprendidas
         * en probabilidades segun el origen y la direccion.
         *
         * EJEMPLO:
         * Si al Este de P se observaron 3 veces R y 1 vez B,
         * la proporcion de R es mayor que la de B en ese contexto.
         * Esto no determina la frecuencia global de R ni de B.
         */
        private double[,,] CalculateProbabilities(int[,,] counts)
        {
            var probabilities = new double[8, tiles.Count, tiles.Count];

            /*
             * TODO 4 - NORMALIZACION DE RELACIONES
             *
             * Obtener la probabilidad de cada tile vecino,
             * condicionada por un tile de origen y una direccion.
             *
             * CONSIDERAR:
             * - Cada distribucion tiene su propio total de ejemplos.
             * - Distinguir conteos absolutos de probabilidades.
             * - Contemplar contextos sin observaciones disponibles.
             * - El resultado debe ser util para Cardinal y Moore.
             */

            return probabilities;
        }

        public double Global(int tile)
        {
            return HasSample ? (double)Frequencies[tile] / Sample.Length : 0;
        }

        public int Total(int direction, int tile)
        {
            int total = 0;

            for (int b = 0; b < tiles.Count; b++)
            {
                total += Counts[direction, tile, b];
            }

            return total;
        }

        public string StatisticsCsv()
        {
            var csv = new StringBuilder();
            csv.AppendLine("tipo,direccion,origen,vecino,frecuencia,probabilidad");

            for (int a = 0; a < tiles.Count; a++)
            {
                string probability = Global(a).ToString("R", CultureInfo.InvariantCulture);
                csv.AppendLine($"global,,{tiles[a].id},,{Frequencies[a]},{probability}");

                for (int d = 0; d < 8; d++)
                {
                    for (int b = 0; b < tiles.Count; b++)
                    {
                        probability = Weights[d, a, b].ToString("R", CultureInfo.InvariantCulture);
                        csv.AppendLine($"par,{Directions[d]},{tiles[a].id},{tiles[b].id},{Counts[d, a, b]},{probability}");
                    }
                }
            }

            return csv.ToString();
        }

        private static string[] SplitCharacters(string text)
        {
            var result = new string[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                result[i] = text[i].ToString();
            }

            return result;
        }
    }
}
