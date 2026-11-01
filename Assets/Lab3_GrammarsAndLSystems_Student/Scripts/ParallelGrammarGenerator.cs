using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class LSystemRule
{
    [Tooltip("Símbolo que será reemplazado durante la expansión.")]
    public string predecessor = "A";

    [Tooltip("Cadena que reemplazará al símbolo.")]
    public string successor = "AB";
}


public class ParallelGrammarGenerator : MonoBehaviour
{
    [Header("Parallel Grammar")]

    [SerializeField]
    private bool autoUpdate = true;


    [Tooltip("Cadena inicial de la gramática.")]
    [SerializeField]
    private string axiom = "A";


    [Tooltip(
        "Reglas de producción utilizadas durante la expansión.\n\n" +
        "Configuración base:\n" +
        "A -> AB\n" +
        "B -> A"
    )]
    [SerializeField]
    private List<LSystemRule> rules =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "A",
                successor = "AB"
            },

            new LSystemRule()
            {
                predecessor = "B",
                successor = "A"
            }
        };


    [Tooltip("Cantidad de iteraciones de expansión.")]
    [Range(0, 10)]
    [SerializeField]
    private int iterations = 4;


    [SerializeField]
    private bool showDerivation = true;


    // -------------------------------------------------------------------------
    // GENERACIÓN DE LA EXPANSIÓN
    // -------------------------------------------------------------------------
    //
    // Una gramática define reglas que permiten reemplazar símbolos por nuevas
    // cadenas.
    //
    // Para la configuración inicial del laboratorio:
    //
    //      Axiom: A
    //
    //      A -> AB
    //      B -> A
    //
    // la expansión esperada es:
    //
    //      Iteración 0: A
    //      Iteración 1: AB
    //      Iteración 2: ABA
    //      Iteración 3: ABAAB
    //      Iteración 4: ABAABABA
    //
    //
    // -------------------------------------------------------------------------
    // REESCRITURA PARALELA
    // -------------------------------------------------------------------------
    //
    // La característica importante de esta primera implementación es que las
    // reglas se aplican de manera PARALELA.
    //
    // Esto significa que todos los símbolos de una iteración son evaluados
    // utilizando la misma cadena de origen.
    //
    // Por ejemplo:
    //
    //      cadena actual:
    //
    //          ABA
    //
    //      reglas:
    //
    //          A -> AB
    //          B -> A
    //
    // Durante esa iteración se evalúa:
    //
    //          A      B      A
    //          |      |      |
    //          AB     A      AB
    //
    // y solamente después de procesar toda la cadena se obtiene:
    //
    //          ABAAB
    //
    // No se debe utilizar el resultado parcial de una sustitución para decidir
    // las sustituciones restantes de la misma iteración.
    //
    //
    // -------------------------------------------------------------------------
    // SÍMBOLOS SIN REGLA
    // -------------------------------------------------------------------------
    //
    // No todos los símbolos necesitan poseer una regla de producción.
    //
    // Si un símbolo no posee una regla asociada, debe conservarse sin cambios.
    //
    // Esto será especialmente importante posteriormente en los L-Systems,
    // donde símbolos como:
    //
    //      +  -  [  ]  &  ^  \  /
    //
    // pueden formar parte de la cadena sin ser necesariamente reemplazados.
    //
    //
    // -------------------------------------------------------------------------
    // DERIVACIÓN
    // -------------------------------------------------------------------------
    //
    // Además de obtener la cadena final, se almacenará el resultado de cada
    // iteración.
    //
    // Esto permite observar en la Console cómo evoluciona la gramática y
    // verificar que las reglas están siendo aplicadas correctamente.
    //

    public static string Generate(
        string axiom,
        List<LSystemRule> rules,
        int iterations,
        List<string> derivation = null)
    {
        //se comienza con el axioma : A
        string current = axiom;

        //Se añade el primero a la lista
        if (derivation != null)
        {
            derivation.Add(current);
        }
        //Se implementa la generación por el número de iteraciones
        for (int i = 0; i < iterations; i++)
        {
            //Se crea la cadena auxiliar
            StringBuilder next = new StringBuilder();
            
            //por cada simbolo en la cadena actual se aplica una regla
            foreach (char symbol in current)
            {
                //se crea una variable auxiliar que referencia al símbolo actual
                string aux = symbol.ToString();

                bool ruleFound = false;

                foreach (LSystemRule rule in rules)
                {
                    //Si el simbolo actual coincide con alguna regla, esta se aplica a la cadena auxiliar
                    if(rule.predecessor == aux)
                    {
                        next.Append(rule.successor);
                        ruleFound = true;
                        break;
                    }

                }
                if (!ruleFound)
                {
                    next.Append(aux);
                }
            }
            //Current recibe el valor de la cadena auxiliar
            current = next.ToString();

            if (derivation != null)
            {
                derivation.Add(current);
            }
        }
        // TODO: EXPANSIÓN PARALELA
        //
        // Implementar la expansión de la gramática considerando:
        //
        // 1. Comenzar desde el axioma.
        // 2. Aplicar las reglas durante la cantidad indicada de iteraciones.
        // 3. Evaluar todos los símbolos de cada iteración de forma paralela.
        // 4. Utilizar la regla correspondiente cuando exista.
        // 5. Mantener sin cambios los símbolos que no posean una regla.
        // 6. Registrar el resultado de cada iteración en derivation.
        // 7. Retornar la cadena obtenida al finalizar.

        return current;
    }


    // -------------------------------------------------------------------------
    // EJECUCIÓN DESDE EL INSPECTOR
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Su función es utilizar los parámetros configurados en el Inspector,
    // ejecutar Generate() y mostrar posteriormente la derivación.
    //

    public void GenerateExpansion()
    {
        List<string> derivation =
            new List<string>();


        string finalSequence =
            Generate(
                axiom,
                rules,
                iterations,
                derivation
            );


        if (showDerivation)
        {
            PrintDerivation(
                derivation,
                finalSequence
            );
        }
    }


    // -------------------------------------------------------------------------
    // VISUALIZACIÓN DE LA DERIVACIÓN
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //

    private void PrintDerivation(
        List<string> derivation,
        string finalSequence)
    {
        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "===== PARALLEL GRAMMAR ====="
        );


        output.AppendLine();


        output.AppendLine(
            "AXIOM:"
        );


        output.AppendLine(
            axiom
        );


        output.AppendLine();


        output.AppendLine(
            "RULES:"
        );


        if (rules == null ||
            rules.Count == 0)
        {
            output.AppendLine(
                "(sin reglas)"
            );
        }
        else
        {
            foreach (LSystemRule rule in rules)
            {
                if (rule == null)
                {
                    continue;
                }


                output.AppendLine(
                    rule.predecessor +
                    " -> " +
                    rule.successor
                );
            }
        }


        output.AppendLine();


        output.AppendLine(
            "DERIVATION:"
        );


        for (int i = 0;
             i < derivation.Count;
             i++)
        {
            output.AppendLine(
                "Iteration " +
                i +
                ": " +
                derivation[i]
            );
        }


        output.AppendLine();


        output.AppendLine(
            "FINAL STRING:"
        );


        output.AppendLine(
            finalSequence
        );


        Debug.Log(
            output.ToString(),
            this
        );
    }
}