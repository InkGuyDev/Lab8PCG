using System;
using UnityEngine;

public static class HeightmapGenerator
{
    public enum InterpolationMode
    {
        None,
        Bilinear,
        Bicubic
    }

    // -------------------------------------------------------------------------
    // RANDOM NOISE
    // -------------------------------------------------------------------------
    // Objetivo:
    // Crear una matriz de alturas donde cada posición reciba un valor
    // pseudoaleatorio independiente en el rango [0, 1].
    //
    // Pseudocódigo:
    // 1. Crear un generador pseudoaleatorio usando "seed".
    // 2. Recorrer todas las posiciones (x, y) del heightmap.
    // 3. Asignar a cada posición un valor aleatorio entre 0 y 1.
    //
    // Importante:
    // La misma semilla debe producir siempre el mismo resultado.
    public static float[,] GenerateRandomNoise(int resolution, int seed)
    {
        //Genaración pseudoaleatoria con variable rand usando seed
        System.Random rand = new System.Random(seed);

        //Se define la matriz (heightmap)
        float[,] heights = new float[resolution, resolution];

        //Se recorre la matriz con posicion x,y
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                //Asignando a cada casilla un valor aleatorio entre 0 y 1 con la función NextDouble
                heights[x, y] = (float)rand.NextDouble();
            }
        }

        //Devuelve la matriz (heightmap)
        return heights;
    }

    // -------------------------------------------------------------------------
    // VALUE NOISE
    // -------------------------------------------------------------------------
    // Objetivo:
    // Generar valores aleatorios únicamente sobre una grilla de puntos de
    // control y calcular los valores intermedios a partir de esos puntos.
    //
    // Si latticeSpacing = 16, por ejemplo, los puntos de control se encuentran
    // separados cada 16 posiciones del heightmap:
    //
    //      0          16          32
    //      O-----------O-----------O
    //      |           |           |
    //      |           |           |
    //      O-----------O-----------O
    //      16
    //
    // Para calcular la altura de una posición (x,y) primero se debe determinar
    // dentro de qué celda de esta grilla se encuentra.
    //
    // Ejemplo:
    //
    //      x = 22
    //      y = 37
    //      latticeSpacing = 16
    //
    //      cellX = 22 / 16 = 1
    //      cellY = 37 / 16 = 2
    //
    // Por lo tanto, el punto pertenece a la celda [1,2].
    //
    // Luego se calcula la posición relativa del punto dentro de esa celda:
    //
    //      tx = (x - inicioX) / latticeSpacing
    //      ty = (y - inicioY) / latticeSpacing
    //
    // Para el ejemplo:
    //
    //      tx = (22 - 1*16) / 16 = 0.375
    //      ty = (37 - 2*16) / 16 = 0.3125
    //
    // tx y ty indican cuánto se ha avanzado dentro de la celda en cada eje
    // y deben encontrarse entre 0 y 1.
    //
    // La posición está rodeada por cuatro puntos de control:
    //
    //              topLeft -------- topRight
    //                 |                 |
    //                 |      (x,y)      |
    //                 |                 |
    //              bottomLeft ----- bottomRight
    //
    // Sus posiciones en la matriz de puntos de control son:
    //
    //      topLeft     -> [cellY,     cellX]
    //      topRight    -> [cellY,     cellX + 1]
    //      bottomLeft  -> [cellY + 1, cellX]
    //      bottomRight -> [cellY + 1, cellX + 1]
    //
    // A partir de estos cuatro valores:
    //
    // 1. Se interpolan horizontalmente topLeft y topRight.
    // 2. Se interpolan horizontalmente bottomLeft y bottomRight.
    // 3. Se interpolan verticalmente los dos resultados anteriores.
    //
    //              topLeft -------- topRight
    //                    \    /
    //                      top
    //                       |
    //                     (x,y)
    //                       |
    //                    bottom
    //                    /    \
    //           bottomLeft ---- bottomRight
    //
    // Para Bilinear se utilizan directamente tx y ty como pesos.
    //
    // Para Bicubic, tx y ty se transforman primero mediante BicubicWeight().
    //
    // El modo None ya está resuelto para poder comparar visualmente el
    // resultado sin interpolación con Bilinear y Bicubic.
    public static float[,] GenerateValueNoise(
        int resolution,
        int latticeSpacing,
        int seed,
        InterpolationMode interpolationMode)
    {
        if (latticeSpacing <= 0)
        {
            throw new ArgumentException("Lattice spacing debe ser mayor que 0.");
        }

        if ((resolution - 1) % latticeSpacing != 0)
        {
            throw new ArgumentException("resolution - 1 debe ser divisible por latticeSpacing.");
        }

        int numberOfCells = (resolution - 1) / latticeSpacing;
        int controlPointResolution = numberOfCells + 1;

        float[,] controlPoints = GenerateControlPoints(controlPointResolution, seed);
        float[,] heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if (interpolationMode == InterpolationMode.None)
                {
                    int nearestX = Mathf.RoundToInt(x / (float)latticeSpacing);
                    int nearestY = Mathf.RoundToInt(y / (float)latticeSpacing);

                    nearestX = Mathf.Clamp(nearestX, 0, controlPointResolution - 1);
                    nearestY = Mathf.Clamp(nearestY, 0, controlPointResolution - 1);

                    heights[y, x] = controlPoints[nearestY, nearestX];
                    continue;
                }


                // TODO: VALUE NOISE
                //
                // Para cada posición (x, y):
                // 1. Determinar en qué celda de la grilla se encuentra.
                // 2. Calcular tx y ty como posición relativa dentro de esa celda.
                //    Ambos valores deben quedar entre 0 y 1.
                // 3. Obtener los cuatro puntos de control:
                //
                //      topLeft -------- topRight
                //         |                 |
                //         |      (x,y)      |
                //         |                 |
                //      bottomLeft ----- bottomRight
                //
                // 4. Obtener los pesos de interpolación para tx y ty.
                // 5. Aplicar interpolación horizontal entre los puntos superiores.
                // 6. Aplicar interpolación horizontal entre los puntos inferiores.
                // 7. Interpolar verticalmente ambos resultados.
                //
                // Para Bilinear se usa directamente t como peso.
                // Para Bicubic se usa el peso generado por BicubicWeight(t).

                //Calcula la coordenada X de la celda actual, como debe ser un valor entero
                //usamos Matf.Min para redondear hacia abajo parte decimal en caso de que el resultado lo tenga
                int cellX = Mathf.Min(x / latticeSpacing, numberOfCells - 1); 

                //Calcula la coordenada Y de la celda actual, como debe ser un valor entero
                //usamos Matf.Min para redondear hacia abajo parte decimal en caso de que el resultado lo tenga
                int cellY = Mathf.Min(y / latticeSpacing, numberOfCells - 1);  

                //tx y ty representan cuanto se ha avanzado en la celda en la que está el punto dentro de la grilla
                //van de 0.0 a 1.0, donde por ejemplo si tx es igual a 0.0 significa que el punto está pegado a la pared izquierda
                //y si es 1.0, está pegado a la pared derecha, ty es lo mismo pero horizontalmente (arriba a abajo)
                float tx = (x - cellX * latticeSpacing) / (float)latticeSpacing;
                float ty = (y - cellY * latticeSpacing) / (float)latticeSpacing;

                //son los cuatro puntos de control de los cuales vamos a obtener la altura del punto en el que estamos
                //estos puntos de control están las esquinas de la celda en la que estamos parados
                float topLeft = controlPoints[cellY, cellX]; //superior izquierda
                float topRight = controlPoints[cellY, cellX + 1]; //superior derecha
                float bottomLeft = controlPoints[cellY + 1, cellX]; //inferior izquierda
                float bottomRight = controlPoints[cellY + 1, cellX + 1]; //inferior derecha

                //determinamos los pesos en ambos ejes x e y, con los cuales luego aplicaremos la interpolacion
                //Ver GetInterpolationWeight para saber como se determina el peso.
                float weightX = GetInterpolationWeight(tx, interpolationMode);
                float weightY = GetInterpolationWeight(ty, interpolationMode);

                //ahora las esquinas superiores e inferiores se interpolan horizontalmente para obtener un valor
                //intermedio mas cercano a nuestro punto, ver LinearInterpolation para saber como se calcula
                float top = LinearInterpolation(topLeft, topRight, weightX);
                float bottom = LinearInterpolation(bottomLeft, bottomRight, weightX);

                //ahora los puntos intermedios se interpolan verticalmente para unir ambas alturas, y asi 
                //determinar el punto final
                heights[y, x] = LinearInterpolation(top, bottom, weightY);
            }
        }

        return heights;
    }

    // -------------------------------------------------------------------------
    // INTERPOLACIÓN LINEAL
    // -------------------------------------------------------------------------
    // Fórmula:
    //
    // L(a, b, t) = a(1 - t) + bt
    //
    // donde:
    // t = 0   -> a
    // t = 1   -> b
    // 0<t<1   -> valor intermedio
    //
    // La interpolación bilineal utiliza esta operación primero sobre el eje X
    // y posteriormente sobre el eje Y.
    public static float LinearInterpolation(float a, float b, float t)
    {
        // TODO: implementar la fórmula de interpolación lineal.

        //esta es la interpolacion lineal, la forma en la que la usamos es la siguiente
        // a siempre sera el lado izquierdo ya sea inferior o superior
        // b el lado derecho
        // t es un porcentaje que representa que tan cerca estamos de a o b, 
        // si es 0 está parado en a
        // si es 1 esta parado en b
        // cualquier valor que no sea 0 o 1 es un valor intermedio entre a o b
        // por eso calculamos los pesos, o weights
        
        float result = (a * (1f - t)) + b*t; //está es la formula con la que calculamos

        return result;
    }

    // -------------------------------------------------------------------------
    // INTERPOLACIÓN BICÚBICA
    // -------------------------------------------------------------------------
    // El capítulo utiliza la siguiente función para suavizar el peso:
    //
    // s(t) = -2t^3 + 3t^2
    //
    // En la interpolación bilineal se utiliza directamente t como peso.
    //
    // En la interpolación bicúbica se calcula primero s(t) y posteriormente
    // se utiliza ese resultado como peso de la misma interpolación.
    //
    // Ejemplo conceptual:
    //
    // Bilinear:
    //      weight = t
    //
    // Bicubic:
    //      weight = s(t)
    //
    // Esto permite que los cambios de altura sean más suaves cerca de los
    // puntos de control.
    public static float BicubicWeight(float t)
    {
        //Aqui ocupamos la formula que salia en los textos
        //La interpolación bicubica suaviza la forma en la que nos movemos por la celda
        //con una interpolacion lineal sera una linea recta
        //usando la interpolacion bicubica obtenemos una curva, o forma de s, que hace que 
        //las montañas se vean mas naturales
        
        //hacemos las exponenciaciones de t
        float firs_t = (float)Math.Pow(t, 3);
        float second_t = (float)Math.Pow(t, 2);

        //Multiplicamos
        float first_term = -2 * firs_t;
        float second_term = 3 * second_t;

        return first_term + second_term; //Devolvemos la suma de ambos terminos, lo que será el peso suavizado 
    }

    // Devuelve el peso que debe utilizar la interpolación.
    //
    // Bilinear -> t
    // Bicubic  -> s(t)
    public static float GetInterpolationWeight(float t, InterpolationMode mode)
    {
        //determina que modo de interpolacion estamos usando para determinar los pesos
        //si es bicubica mandara las distancias (tx y ty) a ser suavizadas
        // si es lineal usara tx y ty directamente como pesos (weights)
        
        if (mode == InterpolationMode.Bicubic)
        {
            return BicubicWeight(t);
        }

        return t;
    }

    // -------------------------------------------------------------------------
    // PUNTOS DE CONTROL
    // -------------------------------------------------------------------------
    // Objetivo:
    // Crear la grilla de valores aleatorios utilizada por Value Noise.
    //
    // A diferencia de Random Noise, estos valores no se generan para cada
    // posición del heightmap. Solo se generan para los puntos de la grilla.
    //
    // Los valores intermedios serán calculados posteriormente mediante
    // interpolación.
    //
    // Pseudocódigo:
    // 1. Crear una matriz "resolution x resolution".
    // 2. Crear un generador pseudoaleatorio utilizando seed.
    // 3. Recorrer la matriz.
    // 4. Asignar un valor pseudoaleatorio [0,1] a cada punto.
    private static float[,] GenerateControlPoints(int resolution, int seed)
    {
        //aca se generan los puntos de control
        //es igual al algoritmo de random noise
        
        System.Random rand = new System.Random(seed);

        float[,] points = new float[resolution, resolution];

        //Se recorre la matriz con posicion x,y
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                //Asignando a cada casilla un valor aleatorio entre 0 y 1 con la función NextDouble
                points[x, y] = (float)rand.NextDouble();
            }
        }

        return points;
    }
}
