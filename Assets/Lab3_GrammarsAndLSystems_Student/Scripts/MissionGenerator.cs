using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class MissionGenerator : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // CONFIGURACIÓN DE LA GENERACIÓN
    // -------------------------------------------------------------------------

    [Header("Generation")]

    [Tooltip(
        "Semilla utilizada para seleccionar las reglas. " +
        "La misma configuración y la misma seed deben producir " +
        "el mismo resultado."
    )]
    [SerializeField]
    private int seed = 12345;


    [Tooltip(
        "Cantidad de expansiones realizadas antes de finalizar la misión."
    )]
    [Range(1, 10)]
    [SerializeField]
    private int expansionSteps = 4;


    [Header("Grammar")]

    [SerializeField]
    private string startSymbol = "M";


    [SerializeField]
    private string startProduction = "STG";


    [SerializeField]
    private string taskSymbol = "T";


    [SerializeField]
    private List<string> taskProductions =
        new List<string>()
        {
            "CT",
            "ET",
            "RT",
            "KTL"
        };


    [SerializeField]
    private string terminalProduction = "C";


    // -------------------------------------------------------------------------
    // GRAMÁTICA SECUENCIAL PARA MISIONES
    // -------------------------------------------------------------------------
    //
    // En el primer ejercicio del laboratorio se implementó una gramática con
    // reescritura PARALELA.
    //
    // En este caso se utilizará una estrategia diferente.
    //
    // La generación de la misión será SECUENCIAL.
    //
    //
    // -------------------------------------------------------------------------
    // DIFERENCIA CON LA EXPANSIÓN PARALELA
    // -------------------------------------------------------------------------
    //
    // En una expansión paralela se procesan todos los símbolos de una cadena
    // antes de obtener la siguiente iteración.
    //
    // En la misión se seleccionará una tarea pendiente y se aplicará una
    // producción sobre ella.
    //
    // Por ejemplo:
    //
    //      M
    //
    // aplicando:
    //
    //      M -> STG
    //
    // se obtiene:
    //
    //      STG
    //
    // El símbolo T representa una tarea todavía pendiente de definir.
    //
    //
    // -------------------------------------------------------------------------
    // PRODUCCIONES DE TAREA
    // -------------------------------------------------------------------------
    //
    // Las producciones disponibles inicialmente son:
    //
    //      T -> CT
    //      T -> ET
    //      T -> RT
    //      T -> KTL
    //
    // Cada una introduce una acción concreta y conserva un símbolo T,
    // permitiendo que la misión continúe creciendo.
    //
    // Por ejemplo:
    //
    //      STG
    //
    // utilizando:
    //
    //      T -> KTL
    //
    // produce:
    //
    //      SKTLG
    //
    // La siguiente expansión podrá volver a actuar sobre la T restante.
    //
    //
    // -------------------------------------------------------------------------
    // SELECCIÓN DE PRODUCCIONES
    // -------------------------------------------------------------------------
    //
    // En cada expansión se seleccionará una de las producciones disponibles.
    //
    // Para que el resultado sea reproducible se utilizará System.Random junto
    // al parámetro seed.
    //
    // De esta manera:
    //
    //      misma seed + mismos parámetros
    //
    // debe producir la misma misión.
    //
    //
    // -------------------------------------------------------------------------
    // FINALIZACIÓN
    // -------------------------------------------------------------------------
    //
    // Después de ejecutar expansionSteps, la cadena todavía puede contener T.
    //
    // Para completar la misión se utilizará:
    //
    //      terminalProduction
    //
    // cuya configuración inicial corresponde a:
    //
    //      T -> C
    //
    // Al finalizar no deberían permanecer tareas pendientes representadas por T.
    //
    //
    // -------------------------------------------------------------------------
    // INTERPRETACIÓN
    // -------------------------------------------------------------------------
    //
    // Al igual que en el L-System, generar la cadena simbólica y darle
    // significado son procesos diferentes.
    //
    // Por ejemplo:
    //
    //      S C K L G
    //
    // puede interpretarse como:
    //
    //      S -> Comienza la misión.
    //      C -> Derrota a los enemigos.
    //      K -> Obtén una llave.
    //      L -> Abre una cerradura.
    //      G -> Completa el objetivo principal.
    //
    // La función GetDescription() que realiza esta traducción ya se encuentra
    // implementada.
    //
    //
    public void GenerateMission()
    {
        if (!ValidateGrammar())
        {
            return;
        }


        // TODO: GENERACIÓN SECUENCIAL DE LA MISIÓN
        //
        // Implementar el proceso completo considerando:
        //
        // 1. Comenzar desde startSymbol y aplicar startProduction.
        // 2. Realizar expansionSteps expansiones secuenciales.
        // 3. Seleccionar las producciones de taskProductions utilizando seed.
        // 4. Reemplazar solamente una tarea pendiente en cada expansión.
        // 5. Finalizar los símbolos taskSymbol restantes con terminalProduction.
        // 6. Registrar la derivación de la gramática.
        // 7. Interpretar la cadena final utilizando GetDescription().
        // 8. Mostrar en Console:
        //
        //      - reglas
        //      - derivación
        //      - cadena final
        //      - misión interpretada
        //
        // El resultado debe ser reproducible utilizando la misma seed.


        //Current parte con el simobolo original "M"
        string current = startSymbol;
        //Inicializa el random con la seed
        System.Random rand = new System.Random(seed);

        //Lista para mostrar los pasos de la generación
        List<string> derivation = new List<string>();
        derivation.Add(current);
        
        //Current pasa la primera produccion M -> STG
        current = startProduction;
        
        //Se itera según expansionSteps la mision
        for (int i = 0; i < expansionSteps; i++) 
        {
            //Se define la nueva variable para guardar los datos 
            StringBuilder mission = new StringBuilder();

            foreach (char symbol in current) //Se recorre current
            {
                if (symbol.ToString() == taskSymbol) //Se añade una nueva task
                {
                    int index = rand.Next(taskProductions.Count);
                    string newTask = taskProductions[index];
                    mission.Append(newTask);
                }
                else //Se añade el simbolo que estaba
                {
                    mission.Append(symbol);
                }
            }
            current = mission.ToString(); //Current pasa a ser la nueva misión

            derivation.Add(current); //Se añade a derivation
        }


        //TerminalProduction

        //Se inicializa una nueva variable para construir la misión sin simbolos "T" al finalizar
        StringBuilder terminalMission = new StringBuilder();

        foreach (char symbol in current) //Se recorre current
        {
            if (symbol.ToString() == taskSymbol) //Si contiene "T", este pasa a ser "C"
            {
                terminalMission.Append(terminalProduction);
            }
            else //Si no, continua agregando los simbolos de current
            {
                terminalMission.Append(symbol);
            }
        }
        //Current pasa a ser la variable terminalMission
        current = terminalMission.ToString();
        //Se agrega current a derivation
        derivation.Add(current);

        StringBuilder output = new StringBuilder();
        output.AppendLine("===== MISSION GENERATOR =====");
        output.AppendLine($"SEED: {seed}");
        output.AppendLine();

        output.AppendLine("RULES:");
        output.AppendLine($"{startSymbol} -> {startProduction}");
        foreach (string prod in taskProductions)
        {
            output.AppendLine($"{taskSymbol} -> {prod}");
        }
        output.AppendLine($"{taskSymbol} -> {terminalProduction}");
        output.AppendLine();

        output.AppendLine("DERIVATION:");
        for (int i = 0; i < derivation.Count; i++)
        {
            output.AppendLine($"Step {i}: {derivation[i]}");
        }
        output.AppendLine();

        output.AppendLine($"FINAL STRING:\n{current}");
        output.AppendLine();

        // 7. Interpretar la cadena final usando GetDescription()
        output.AppendLine("INTERPRETED MISSION:");
        foreach (char symbol in current)
        {
            string desc = GetDescription(symbol);
            if (!string.IsNullOrEmpty(desc))
            {
                output.AppendLine($"- {desc}");
            }
        }

        Debug.Log(output.ToString(), this);

    }


    // -------------------------------------------------------------------------
    // INTERPRETACIÓN DE SÍMBOLOS
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Cada símbolo terminal posee un significado dentro de la misión.
    //
    private string GetDescription(
        char symbol)
    {
        switch (symbol)
        {
            case 'S':
                return
                    "Comienza la misión.";

            case 'C':
                return
                    "Derrota a los enemigos.";

            case 'E':
                return
                    "Explora la zona.";

            case 'R':
                return
                    "Recolecta el recurso solicitado.";

            case 'K':
                return
                    "Obtén una llave.";

            case 'L':
                return
                    "Utiliza la llave para abrir una cerradura.";

            case 'G':
                return
                    "Completa el objetivo principal.";

            default:
                return null;
        }
    }


    // -------------------------------------------------------------------------
    // VALIDACIÓN
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Su objetivo es detectar configuraciones básicas inválidas antes
    // de comenzar la generación.
    //
    private bool ValidateGrammar()
    {
        if (string.IsNullOrEmpty(
            startSymbol))
        {
            Debug.LogError(
                "Start Symbol no puede estar vacío.",
                this
            );

            return false;
        }


        if (string.IsNullOrEmpty(
            startProduction))
        {
            Debug.LogError(
                "Start Production no puede estar vacía.",
                this
            );

            return false;
        }


        if (string.IsNullOrEmpty(
            taskSymbol))
        {
            Debug.LogError(
                "Task Symbol no puede estar vacío.",
                this
            );

            return false;
        }


        if (taskProductions == null ||
            taskProductions.Count == 0)
        {
            Debug.LogError(
                "Debe existir al menos una producción para Task.",
                this
            );

            return false;
        }


        if (string.IsNullOrEmpty(
            terminalProduction))
        {
            Debug.LogError(
                "Terminal Production no puede estar vacía.",
                this
            );

            return false;
        }


        if (terminalProduction.Contains(
            taskSymbol))
        {
            Debug.LogError(
                "La producción terminal no debe volver a generar " +
                "el símbolo '" +
                taskSymbol +
                "'.",
                this
            );

            return false;
        }


        return true;
    }
}