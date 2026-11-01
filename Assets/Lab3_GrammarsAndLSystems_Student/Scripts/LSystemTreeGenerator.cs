using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystemTreeGenerator : MonoBehaviour
{
    public enum GenerationMode
    {
        TwoD,
        ThreeD
    }


    // -------------------------------------------------------------------------
    // CONFIGURACIÓN
    // -------------------------------------------------------------------------

    [Header("Generation")]

    [SerializeField]
    private GenerationMode generationMode =
        GenerationMode.TwoD;

    [SerializeField]
    private bool autoUpdate = true;


    [Header("2D Configuration")]

    [SerializeField]
    private string axiom2D = "F";

    [SerializeField]
    private List<LSystemRule> rules2D =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "F",
                successor = "F[+F]F[-F]F"
            }
        };

    [Range(0, 5)]
    [SerializeField]
    private int iterations2D = 3;

    [Range(1f, 90f)]
    [SerializeField]
    private float angle2D = 25f;


    [Header("3D Configuration")]

    [SerializeField]
    private string axiom3D = "F";

    [SerializeField]
    private List<LSystemRule> rules3D =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "F",
                successor = "F[+F][-F][&F][^F]"
            }
        };

    [Range(0, 5)]
    [SerializeField]
    private int iterations3D = 3;

    [Range(1f, 90f)]
    [SerializeField]
    private float angle3D = 30f;


    [Header("Shared Visualization")]

    [Min(0.05f)]
    [SerializeField]
    private float segmentLength = 1f;

    [Min(0.005f)]
    [SerializeField]
    private float branchRadius = 0.06f;

    [SerializeField]
    private Material branchMaterial;


    [Header("Height Color")]

    [SerializeField]
    private bool useHeightColor = true;

    [SerializeField]
    private Color lowColor =
        new Color(
            0.35f,
            0.17f,
            0.06f,
            1f
        );

    [SerializeField]
    private Color highColor =
        new Color(
            0.15f,
            0.55f,
            0.18f,
            1f
        );

    [Min(0.1f)]
    [SerializeField]
    private float colorHeight = 8f;


    [Header("Debug")]

    [SerializeField]
    private bool showDerivation = true;


    [HideInInspector]
    [SerializeField]
    private Transform generatedRoot;

    private int branchCounter = 0;


    // -------------------------------------------------------------------------
    // ESTADO DE LA TORTUGA
    // -------------------------------------------------------------------------
    //
    // Turtle Graphics interpreta una cadena como una secuencia de instrucciones.
    //
    // Para poder recorrer dicha cadena es necesario mantener el estado actual
    // de la "tortuga".
    //
    // En este laboratorio el estado está compuesto por:
    //
    //      position
    //
    //          posición actual desde la cual continuará el crecimiento.
    //
    //      rotation
    //
    //          orientación actual que determina la dirección del siguiente
    //          movimiento.
    //
    // Ambos valores son importantes al trabajar con ramificaciones.
    //
    private struct TurtleState
    {
        public Vector3 position;
        public Quaternion rotation;

        public TurtleState(
            Vector3 position,
            Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }


    // -------------------------------------------------------------------------
    // GENERACIÓN DEL L-SYSTEM
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // El proceso posee dos etapas diferentes:
    //
    //      1. GENERACIÓN SIMBÓLICA
    //
    //          ParallelGrammarGenerator.Generate(...)
    //
    //          produce una cadena mediante las reglas implementadas en
    //          el ejercicio anterior.
    //
    //
    //      2. INTERPRETACIÓN
    //
    //          Interpret(...)
    //
    //          transforma los símbolos de esa cadena en movimientos y
    //          rotaciones.
    //
    //
    // Esta separación es importante:
    //
    // La gramática NO genera directamente la geometría.
    //
    // Primero genera una representación simbólica y posteriormente dicha
    // representación es interpretada.
    //
    public void GenerateTree()
    {
        ClearGeneratedTree();
        EnsureGeneratedRoot();

        branchCounter = 0;

        string activeAxiom;
        List<LSystemRule> activeRules;
        int activeIterations;
        float activeAngle;


        if (generationMode ==
            GenerationMode.TwoD)
        {
            activeAxiom =
                axiom2D;

            activeRules =
                rules2D;

            activeIterations =
                iterations2D;

            activeAngle =
                angle2D;
        }
        else
        {
            activeAxiom =
                axiom3D;

            activeRules =
                rules3D;

            activeIterations =
                iterations3D;

            activeAngle =
                angle3D;
        }


        List<string> derivation =
            new List<string>();


        string sequence =
            ParallelGrammarGenerator.Generate(
                activeAxiom,
                activeRules,
                activeIterations,
                derivation
            );


        if (showDerivation)
        {
            PrintDerivation(
                activeAxiom,
                activeRules,
                derivation,
                sequence
            );
        }


        Interpret(
            sequence,
            activeAngle
        );
    }


    // -------------------------------------------------------------------------
    // TURTLE GRAPHICS
    // -------------------------------------------------------------------------
    //
    // La cadena generada anteriormente contiene símbolos que ahora deben ser
    // interpretados como instrucciones.
    //
    //
    // -------------------------------------------------------------------------
    // MOVIMIENTO
    // -------------------------------------------------------------------------
    //
    // F:
    //
    //      avanzar en la dirección actual y dibujar un segmento.
    //
    // f:
    //
    //      avanzar la misma distancia pero sin dibujar.
    //
    // La dirección depende de la orientación actual de la tortuga.
    //
    //
    // -------------------------------------------------------------------------
    // ROTACIÓN 2D
    // -------------------------------------------------------------------------
    //
    // + y - modifican la orientación utilizando el ángulo configurado.
    //
    // Ambos representan rotaciones opuestas.
    //
    // Esto permite que futuros movimientos F continúen en otra dirección.
    //
    //
    // -------------------------------------------------------------------------
    // RAMIFICACIONES
    // -------------------------------------------------------------------------
    //
    // Los símbolos [ y ] permiten generar estructuras ramificadas.
    //
    // Al encontrar:
    //
    //      [
    //
    // se debe recordar el estado actual.
    //
    // Posteriormente:
    //
    //      ]
    //
    // permite volver a ese estado y continuar desde allí.
    //
    // Debido a que pueden existir ramas dentro de otras ramas, los estados
    // deben almacenarse utilizando una pila:
    //
    //      Stack<TurtleState>
    //
    // El último estado almacenado debe ser el primero en recuperarse.
    //
    //
    // -------------------------------------------------------------------------
    // EXTENSIÓN 3D
    // -------------------------------------------------------------------------
    //
    // En modo 2D las rotaciones mantienen la estructura sobre un plano.
    //
    // En modo ThreeD se incorporan nuevas rotaciones:
    //
    //      & y ^       Pitch
    //
    //      \ y /       Roll
    //
    // Estas rotaciones permiten cambiar la orientación sobre ejes adicionales
    // y generar ramas fuera del plano original.
    //
    // Los operadores adicionales solamente deben actuar en modo ThreeD.
    //
    //
    // -------------------------------------------------------------------------
    // QUATERNIONS
    // -------------------------------------------------------------------------
    //
    // Unity representa la orientación utilizando Quaternion.
    //
    // Para aplicar las rotaciones puede utilizarse:
    //
    //      Quaternion.AngleAxis(...)
    //
    // y combinar la nueva rotación con la orientación actual.
    //
    // La figura incluida en la guía muestra como referencia los movimientos
    // yaw, pitch y roll.
    //
    //
    private void Interpret(
        string sequence,
        float activeAngle)
    {
        // TODO: TURTLE GRAPHICS 2D Y 3D
        //
        // Recorrer los símbolos de sequence e implementar:
        //
        // 1. El estado inicial de la tortuga.
        // 2. Movimiento con y sin dibujo mediante F y f.
        // 3. Rotaciones 2D mediante + y -.
        // 4. Guardado y recuperación del estado mediante [ y ].
        // 5. Rotaciones adicionales del modo 3D mediante &, ^, \ y /.
        //
        // Utilizar:
        //
        //      Stack<TurtleState>
        //      CreateBranch(...)
        //      Quaternion
        //
        // Considerar que posición y orientación deben recuperarse juntas
        // al retornar desde una rama.

        //Creamos una nueva pila para la tortuga e inicializamos los parametros de posicion y rotacion
        Stack<TurtleState> stateStack = new Stack<TurtleState>();
        Vector3 currentPosition = Vector3.zero;
        Quaternion currentRotation = Quaternion.identity;

        //Recorremos cada caracter en la cadena que recibe la funcion
        foreach (char c in sequence)
        {
            switch (c) //dependiendo del caracter ocuparemos un comportamiento distinto
            {
                //F avanza en la posicion actual 
                case 'F':
                    //aquí avanzamos a la siguiente posicion
                    Vector3 nextPosDraw = currentPosition + currentRotation * (Vector3.up * segmentLength);
                    //dibujamos la rama con usando los parametros de la posicion anterior y la posicion 
                    //que acabamos de calcular
                    CreateBranch(currentPosition, nextPosDraw);
                    //cambiamos nuestra posicion actual
                    currentPosition = nextPosDraw;
                    break;

                //f hace lo mismo que F pero no dibuja la rama
                case 'f':
                    Vector3 nextPosMove = currentPosition + currentRotation * (Vector3.up * segmentLength);
                    currentPosition = nextPosMove;
                    break;

                //+ hace una rotacion hacia la izquierda
                case '+':
                    currentRotation *= Quaternion.AngleAxis(activeAngle, Vector3.forward);
                    break;

                //- hace una rotacion hacia la derecha
                case '-':
                    currentRotation *= Quaternion.AngleAxis(-activeAngle, Vector3.forward);
                    break;

                //[ crea un nuevo TurtleState con los parametros de posicion y rotacion actuales
                //y lo incertamos a la pila, de esta forma estamos guardando un "estado" de nuestra
                //tortuga
                case '[':
                    stateStack.Push(new TurtleState(currentPosition, currentRotation));
                    break;

                //] carga el ultimo "estado" guardado de la tortuga, regresando a una posicion
                //y rotacion anteriores, esto permite seguir dibujando el arbol con normalidad
                //luego de desviarse por alguna rama
                case ']':
                    if (stateStack.Count > 0) //importante revisar si existen "estados" en la pila
                    {
                        TurtleState prevState = stateStack.Pop();
                        currentPosition = prevState.position;
                        currentRotation = prevState.rotation;
                    }
                    break;

                //& hace un pitch positivo (rotacion alrededor del eje x)
                case '&':
                    if (generationMode == GenerationMode.ThreeD)
                        currentRotation *= Quaternion.AngleAxis(activeAngle, Vector3.right); 
                    break;

                //^ hace un pitch negativo (rotacion alrededor del eje x)
                case '^':
                    if (generationMode == GenerationMode.ThreeD)
                        currentRotation *= Quaternion.AngleAxis(-activeAngle, Vector3.right); 
                    break;

                // \ hace un roll positivo (rotacion alrededor del eje y)
                case '\\':
                    if (generationMode == GenerationMode.ThreeD)
                        currentRotation *= Quaternion.AngleAxis(activeAngle, Vector3.up); 
                    break;

                // / hace un roll negativo (rotacion alrededor del eje y)
                case '/':
                    if (generationMode == GenerationMode.ThreeD)
                        currentRotation *= Quaternion.AngleAxis(-activeAngle, Vector3.up); 
                    break;
            }
        }
    }


    // -------------------------------------------------------------------------
    // CREACIÓN DE SEGMENTOS
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // CreateBranch() recibe dos posiciones y crea un cilindro entre ellas.
    //
    // La lógica de visualización no forma parte de la implementación evaluada.
    //
    private void CreateBranch(
        Vector3 start,
        Vector3 end)
    {
        Vector3 direction =
            end - start;

        float length =
            direction.magnitude;

        if (length <= 0f)
        {
            return;
        }


        GameObject branch =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        branchCounter++;

        branch.name =
            "Branch_" +
            branchCounter.ToString("0000");


        branch.transform.SetParent(
            generatedRoot,
            false
        );


        Vector3 middlePoint =
            (start + end) *
            0.5f;


        branch.transform.localPosition =
            middlePoint;


        branch.transform.localRotation =
            Quaternion.FromToRotation(
                Vector3.up,
                direction.normalized
            );


        branch.transform.localScale =
            new Vector3(
                branchRadius,
                length * 0.5f,
                branchRadius
            );


        Renderer renderer =
            branch.GetComponent<Renderer>();


        if (renderer != null &&
            branchMaterial != null)
        {
            renderer.sharedMaterial =
                branchMaterial;
        }


        if (renderer != null &&
            useHeightColor)
        {
            ApplyHeightColor(
                renderer,
                middlePoint.y
            );
        }


        Collider collider =
            branch.GetComponent<Collider>();

        if (collider != null)
        {
            SafeDestroy(
                collider
            );
        }
    }


    // -------------------------------------------------------------------------
    // COLOR SEGÚN ALTURA
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    private void ApplyHeightColor(
        Renderer renderer,
        float height)
    {
        float safeHeight =
            Mathf.Max(
                0.1f,
                colorHeight
            );


        float t =
            Mathf.InverseLerp(
                0f,
                safeHeight,
                height
            );


        Color branchColor =
            Color.Lerp(
                lowColor,
                highColor,
                t
            );


        MaterialPropertyBlock block =
            new MaterialPropertyBlock();


        renderer.GetPropertyBlock(
            block
        );


        Material material =
            renderer.sharedMaterial;


        if (material != null)
        {
            if (material.HasProperty(
                "_BaseColor"))
            {
                block.SetColor(
                    "_BaseColor",
                    branchColor
                );
            }


            if (material.HasProperty(
                "_Color"))
            {
                block.SetColor(
                    "_Color",
                    branchColor
                );
            }
        }


        renderer.SetPropertyBlock(
            block
        );
    }


    // -------------------------------------------------------------------------
    // INFRAESTRUCTURA DE VISUALIZACIÓN
    // -------------------------------------------------------------------------

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
        {
            return;
        }


        Transform existing =
            transform.Find(
                "Generated Tree"
            );


        if (existing != null)
        {
            generatedRoot =
                existing;

            return;
        }


        GameObject root =
            new GameObject(
                "Generated Tree"
            );


        root.transform.SetParent(
            transform,
            false
        );


        root.transform.localPosition =
            Vector3.zero;

        root.transform.localRotation =
            Quaternion.identity;

        root.transform.localScale =
            Vector3.one;


        generatedRoot =
            root.transform;
    }


    public void ClearGeneratedTree()
    {
        if (generatedRoot == null)
        {
            Transform existing =
                transform.Find(
                    "Generated Tree"
                );


            if (existing != null)
            {
                generatedRoot =
                    existing;
            }
        }


        if (generatedRoot != null)
        {
            SafeDestroy(
                generatedRoot.gameObject
            );

            generatedRoot =
                null;
        }


        branchCounter = 0;
    }


    // -------------------------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------------------------

    private void PrintDerivation(
        string activeAxiom,
        List<LSystemRule> activeRules,
        List<string> derivation,
        string finalSequence)
    {
        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "===== L-SYSTEM TREE ====="
        );


        output.AppendLine();

        output.AppendLine(
            "MODE:"
        );

        output.AppendLine(
            generationMode.ToString()
        );


        output.AppendLine();

        output.AppendLine(
            "AXIOM:"
        );

        output.AppendLine(
            activeAxiom
        );


        output.AppendLine();

        output.AppendLine(
            "RULES:"
        );


        foreach (LSystemRule rule in activeRules)
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


    private void SafeDestroy(
        Object target)
    {
        if (target == null)
        {
            return;
        }


        if (Application.isPlaying)
        {
            Destroy(
                target
            );
        }
        else
        {
            DestroyImmediate(
                target
            );
        }
    }
}