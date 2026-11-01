LABORATORIO 3 — GRAMÁTICAS Y L-SYSTEMS
=======================================

OBJETIVO
--------
Implementar y comprender tres usos relacionados con gramáticas para generación
procedural:

1. Una gramática con expansión paralela.
2. Un L-System que reutiliza dicha expansión y utiliza Turtle Graphics para
   generar estructuras ramificadas en 2D y 3D.
3. Una gramática secuencial para generar la estructura simbólica de una misión
   y convertirla posteriormente en una secuencia de acciones.

La carpeta está preparada para que los scripts compilen desde el inicio.
Las secciones que deben ser implementadas se encuentran identificadas mediante
comentarios TODO y pseudocódigo de apoyo.


IMPORTANTE
----------
Los scripts de la carpeta Editor corresponden a infraestructura del laboratorio.

No forman parte de la implementación evaluada y no deben modificarse para
reemplazar, evitar o simular la lógica solicitada.

La implementación evaluada se concentra en:

- Scripts/ParallelGrammarGenerator.cs
- Scripts/LSystemTreeGenerator.cs
- Scripts/MissionGenerator.cs


ESTRUCTURA DE LA CARPETA
------------------------
Lab3_GrammarsAndLSystems_Student/
├── README_Lab3.txt
├── Lab3_GrammarsAndLSystems.tex
├── Editor/
│   ├── ParallelGrammarGeneratorEditor.cs
│   ├── LSystemTreeGeneratorEditor.cs
│   └── MissionGeneratorEditor.cs
└── Scripts/
    ├── ParallelGrammarGenerator.cs
    ├── LSystemTreeGenerator.cs
    └── MissionGenerator.cs


CONFIGURACIÓN EN UNITY
----------------------
1. Copiar esta carpeta dentro del proyecto Unity, por ejemplo:

   Assets/Laboratorios/Lab3/GrammarsAndLSystems/

2. Crear o abrir una escena para el laboratorio.

3. Crear tres GameObjects vacíos:

   Lab3_GrammarsAndLSystems
   ├── 01_ParallelGrammar
   ├── 02_LSystemTree
   └── 03_MissionGenerator

4. Agregar ParallelGrammarGenerator a 01_ParallelGrammar.

5. Agregar LSystemTreeGenerator a 02_LSystemTree.

6. Agregar MissionGenerator a 03_MissionGenerator.

7. Los scripts de Editor agregarán automáticamente botones y controles
   adicionales al Inspector.

No es necesario entrar en Play Mode para probar los generadores.


PARALLEL GRAMMAR GENERATOR
--------------------------
Este primer ejercicio implementa la expansión paralela que posteriormente será
reutilizada por el L-System.

Configuración base:

    Axiom: A

    A -> AB
    B -> A

    Iterations: 4

Resultado esperado:

    Iteración 0: A
    Iteración 1: AB
    Iteración 2: ABA
    Iteración 3: ABAAB
    Iteración 4: ABAABABA

En una expansión PARALELA, todos los símbolos de la cadena actual se evalúan
antes de construir la cadena correspondiente a la siguiente iteración.

Por ejemplo:

    ABA

se evalúa utilizando:

    A -> AB
    B -> A
    A -> AB

produciendo:

    ABAAB

Los símbolos que no posean una regla asociada deben conservarse sin cambios.


L-SYSTEM TREE GENERATOR
-----------------------
El L-System reutiliza la expansión implementada en:

    ParallelGrammarGenerator.Generate(...)

La cadena resultante se interpreta posteriormente mediante Turtle Graphics.

El Inspector permite cambiar entre TwoD y ThreeD.
Cada modo posee una configuración independiente.


Configuración base 2D:

    Axiom: F
    Rule: F -> F[+F]F[-F]F
    Iterations: 3
    Angle: 25


Configuración base 3D:

    Axiom: F
    Rule: F -> F[+F][-F][&F][^F]
    Iterations: 3
    Angle: 30


Símbolos de Turtle Graphics:

    F    avanzar dibujando
    f    avanzar sin dibujar

    +    rotación positiva
    -    rotación negativa

    [    guardar posición y orientación
    ]    recuperar posición y orientación

    &    pitch positivo
    ^    pitch negativo

    \    roll positivo
    /    roll negativo

Los símbolos &, ^, \ y / corresponden a la extensión 3D y solamente deben
actuar cuando el generador se encuentre en modo ThreeD.

Otros símbolos pueden existir dentro de una gramática aunque no posean una
interpretación gráfica directa.


AUTO UPDATE
-----------
Auto Update permite ejecutar nuevamente los generadores al modificar parámetros
desde el Inspector.

Durante el desarrollo se recomienda mantenerlo activado para observar de forma
inmediata el efecto de cambios como:

Parallel Grammar:
- Axiom.
- Rules.
- Iterations.

L-System:
- Mode.
- Axiom.
- Rules.
- Iterations.
- Angle.
- Segment Length.
- Branch Radius.

Los botones de generación manual continúan disponibles.


COLOR SEGÚN ALTURA
------------------
La infraestructura visual del L-System permite modificar el color de las ramas
según su altura.

Low Color representa la zona inferior y High Color la zona superior.
Color Height determina la altura a la que se alcanza completamente High Color.

Esta característica es solamente visual y NO forma parte de los requisitos
evaluados.


TODO PRINCIPALES
----------------
Los TODO se encuentran dentro de los scripts evaluados.

ParallelGrammarGenerator.cs
    Implementar la expansión paralela utilizando el axioma, las reglas y la
    cantidad de iteraciones configuradas.

    Debe soportar múltiples reglas y conservar los símbolos que no posean una
    producción asociada.


LSystemTreeGenerator.cs
    Implementar la interpretación mediante Turtle Graphics.

    Se debe considerar:

    - F y f para desplazamiento.
    - + y - para las rotaciones del modo 2D.
    - [ y ] para guardar y recuperar el estado de la tortuga.
    - &, ^, \ y / para las rotaciones adicionales del modo 3D.

    El estado de la tortuga debe considerar posición y orientación.


MissionGenerator.cs
    Implementar la generación secuencial de la misión.

    Se debe considerar:

    - Aplicar la producción inicial.
    - Seleccionar producciones utilizando seed.
    - Realizar expansionSteps expansiones.
    - Reemplazar una única tarea pendiente en cada paso.
    - Finalizar las tareas restantes.
    - Interpretar la cadena resultante como una secuencia de acciones.


MISSION GENERATOR
-----------------
Configuración base:

    M -> STG

    T -> CT
    T -> ET
    T -> RT
    T -> KTL

    T -> C     [finalización]


Símbolos:

    M = Mission
    S = Start
    T = Task pendiente
    C = Combat
    E = Explore
    R = Resource
    K = Key
    L = Lock
    G = Goal


La Console debe mostrar:

1. Reglas de la gramática.
2. Derivación paso a paso.
3. Cadena final.
4. Misión interpretada.

A diferencia del L-System, la generación de la misión utiliza reescritura
SECUENCIAL: en cada paso se reemplaza solamente una ocurrencia de T.

La selección de las producciones debe utilizar seed, de manera que la misma
configuración produzca nuevamente el mismo resultado.


PUNTAJE
-------
1. Expansión paralela ......................................... 1,0 punto

2. L-System mediante Turtle Graphics
   - Implementación 2D ........................................ 1,0 punto
   - Extensión 3D ............................................. 2,0 puntos

3. Generación de misión mediante gramática secuencial ......... 2,0 puntos

TOTAL ......................................................... 6,0 puntos


ENTREGA
-------
Se deberán entregar únicamente las versiones modificadas de:

- ParallelGrammarGenerator.cs
- LSystemTreeGenerator.cs
- MissionGenerator.cs

No es necesario entregar los scripts de Editor ni el proyecto completo.

Formato sugerido:

    Lab3_Apellido1_Apellido2.zip

En grupos de tres integrantes se debe incorporar también el apellido del tercer
integrante.


DEFENSA
-------
Todo integrante debe comprender la totalidad del código entregado.

Durante la defensa se puede solicitar, entre otras acciones:

- Explicar cómo funciona la expansión paralela.
- Modificar un axioma o una regla de producción.
- Explicar qué ocurre con un símbolo que no posee regla.
- Cambiar entre TwoD y ThreeD.
- Modificar Angle o Iterations y anticipar su efecto.
- Explicar por qué [ y ] requieren almacenar estados.
- Explicar por qué posición y orientación forman parte del estado.
- Explicar las diferencias entre yaw, pitch y roll.
- Explicar la diferencia entre reescritura paralela y secuencial.
- Cambiar seed o expansionSteps en MissionGenerator.
- Explicar por qué K aparece antes que L en una producción como KTL.
- Realizar una modificación simple sobre el código.

La versión entregada en Educandus será la utilizada para la defensa.