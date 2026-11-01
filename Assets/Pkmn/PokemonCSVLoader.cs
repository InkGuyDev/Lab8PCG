using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class PokemonData
{
    public string name;
    public string type1;
    public string type2;

    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int bst;
}

public class PokemonCSVLoader : MonoBehaviour
{
    [Header("CSV")]
    [SerializeField] private TextAsset csvFile;
    [SerializeField] private bool loadOnAwake = true;

    private readonly List<PokemonData> pokemon = new List<PokemonData>();

    public IReadOnlyList<PokemonData> Data => pokemon;
    public bool IsLoaded => pokemon.Count > 0;

    private void Awake()
    {
        if (loadOnAwake)
            Load();
    }

    /*
     * ============================================================
     * OBJETIVO
     * ============================================================
     *
     * El CSV contiene muchas columnas, pero para este laboratorio
     * solo necesitamos una representación reducida:
     *
     * Name, Type 1, Type 2, HP, Att, Spa, Def, Spd, Spe y BST.
     *
     * A partir de esos valores construiremos:
     *
     * offense = max(Att, Spa)
     * defense = max(Def, Spd)
     *
     * EJEMPLO
     * ------------------------------------------------------------
     * Una fila del CSV puede contener:
     *
     * Name = "Charizard"
     * Type 1 = "Fire"
     * Type 2 = "Flying"
     * HP = 78
     * Att = 84
     * Spa = 109
     * Def = 78
     * Spd = 85
     * Spe = 100
     * BST = 534
     *
     * El PokemonData resultante será:
     *
     * {
     *   name = "Charizard",
     *   type1 = "Fire",
     *   type2 = "Flying",
     *   hp = 78,
     *   offense = 109,   // max(84, 109)
     *   defense = 85,    // max(78, 85)
     *   speed = 100,
     *   bst = 534
     * }
     *
     * PSEUDOCÓDIGO
     * ------------------------------------------------------------
     * 1: clear pokemon list
     * 2: verify that csvFile exists
     * 3: split file into lines
     * 4: read first line as header
     * 5: create a map: column name -> column index
     * 6: for each data row:
     * 7:      parse row
     * 8:      read Name
     * 9:      read Type 1 and Type 2
     * 10:     read HP
     * 11:     read Att and Spa
     * 12:     read Def and Spd
     * 13:     read Spe
     * 14:     read BST
     * 15:     create PokemonData
     * 16:     offense = max(Att, Spa)
     * 17:     defense = max(Def, Spd)
     * 18:     add PokemonData to pokemon list
     *
     * RESULTADO ESPERADO
     * ------------------------------------------------------------
     * pokemon = [
     *   PokemonData("Bulbasaur", ...),
     *   PokemonData("Ivysaur", ...),
     *   PokemonData("Venusaur", ...),
     *   ...
     * ]
     *
     * pokemon.Count corresponde a la cantidad de filas válidas.
     */

    [ContextMenu("Load CSV")]
    public void Load() 
    {
        String[] lines; //variable para guardar el contenido del archivo csv en lineas

        // vaciamos la lista interna . 
        // esto previene que se dupliquen los datos o existan problemas de memoria
        pokemon.Clear();

        // verificamos la existencia del archivo  
        if (csvFile != null)
        {
            Debug.Log("El archivo existe");

            // para guardar el texto del archivo csv en la variable declarada al principio
            // se divide en un arreglo de strings usando el salto de linea como delimitador.
            // StringSplitOptions.RemoveEmptyEntries evita procesar lineas vacias al final del
            // archivo que causarían excepciones de indice
            lines = csvFile.text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // se extrae la primera fila (índice 0), se limpia el retorno de carro oculto con \r (el ayudante me dijo) 
            // se usa el metodo implementado para realizar el parseamiento
            List<string> headers = ParseCsvLine(lines[0].TrimEnd('\r'));

            // usamos el metodo implementado para crear las columnas del mapa,
            // obteniendo la id y el nombre correspondientes
            Dictionary<string, int> map = BuildColumnMap(headers);

            //aqui obtenemos las id cada una de las columnas del mapa buscandolas por su nombre
            //NOTA: sabemos que el archivo que estamos usando contiene las columnas correspondientes
            //se podria añadir una comprobacion mediante map.ContainsKey para saber que las columnas
            //existen, y asi evitar excepciones o cargar datos nulos
            int nameIdx = map["Name"];
            int type1Idx = map["Type 1"];
            int type2Idx = map["Type 2"];
            int hpIdx = map["HP"];
            int attIdx = map["Att"];
            int spaIdx = map["Spa"];
            int defIdx = map["Def"];
            int spdIdx = map["Spd"];
            int speIdx = map["Spe"];
            int bstIdx = map["BST"];

            //  el bucle for itera sobre los registros empezando desde i = 1
            //  para evitar la cabecera del cvs.
            for (int i = 1; i < lines.Length; i++)
            {
                // para leer cada uno de los datos (entiendase nombre del pkm, tipos, etc, etc)
                // se necesita parsear la linea correspondiente a ese pkm (estamos leyendo más de 1000 pokemon)
                // asi que realizaremos el parseo de inmediato para evitar hacerlo cada vez que vayamos a registrar
                // uno de los datos
                List<string> rowValues = ParseCsvLine(lines[i].TrimEnd('\r'));

                // leemos cada dato de los pkm, dependiendo del tipo de dato usaremos una funcion
                // dependiendo si corresponde a texto o valor numerico (ReadString y ReadInt respectivamente)
                // como ya parseamos usando rowValues la usaremos como parametro
                string name = ReadString(rowValues, nameIdx);
                string type1 = ReadString(rowValues, type1Idx);
                string type2 = ReadString(rowValues, type2Idx);
                int hp = ReadInt(rowValues, hpIdx);
                int att = ReadInt(rowValues, attIdx);
                int spa = ReadInt(rowValues, spaIdx);
                int def = ReadInt(rowValues, defIdx);
                int spd = ReadInt(rowValues, spdIdx);
                int spe = ReadInt(rowValues, speIdx);
                int bst = ReadInt(rowValues, bstIdx);

                // creamos el objeto pokemon y le asignamos a sus atributos los datos que obtuvimos
                // buscando en el cvs 
                PokemonData pkmnData = new PokemonData
                {
                    name = name,
                    type1 = type1,
                    type2 = type2,
                    hp = hp,
                    speed = spe,
                    bst = bst,

                    //la ofensiva es el maximo del ataque fisico y el especial
                    offense = Math.Max(att, spa),

                    //la defensa es lo mismo pero con las defensas (bruh)
                    defense = Math.Max(def, spd)
                };

                // ya creado el pokemon lo agregamos a la lista
                pokemon.Add(pkmnData);
            }

            //en teoria luego iterar por todas las lineas deberiamos tener 1024 pkm
            //(mentira, en el archivo son 1032 pq las versiones de alola estan por separado sdjksd)
            Debug.Log(pokemon.Count);
        }
        else
        {
            Debug.Log("El archivo no existe o no está asignado"); 
        }
    }

    // ============================================================
    // INFRAESTRUCTURA ENTREGADA
    // No es necesario modificar los métodos siguientes.
    // ============================================================

    private static Dictionary<string, int> BuildColumnMap(List<string> headers)
    {
        Dictionary<string, int> result =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            string header = headers[i].Trim();

            if (!result.ContainsKey(header))
                result.Add(header, i);
        }

        return result;
    }

    private static string ReadString(List<string> values, int index)
    {
        if (index < 0 || index >= values.Count)
            return string.Empty;

        return values[index].Trim();
    }

    private static int ReadInt(List<string> values, int index)
    {
        string raw = ReadString(values, index);

        if (int.TryParse(
            raw,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int value))
        {
            return value;
        }

        return 0;
    }

    /// <summary>
    /// Parser CSV entregado.
    /// Permite leer campos entre comillas y comas internas.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        List<string> values = new List<string>();
        StringBuilder current = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());

        return values;
    }
}
