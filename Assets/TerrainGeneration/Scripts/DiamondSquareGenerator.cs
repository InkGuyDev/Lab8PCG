using UnityEngine;

public static class DiamondSquareGenerator
{
    // -------------------------------------------------------------------------
    // DIAMOND-SQUARE
    // -------------------------------------------------------------------------
    //
    // Objetivo:
    // Generar un terreno mediante subdivisiones sucesivas, agregando nuevas
    // alturas a escalas cada vez menores.
    //
    // A diferencia de los métodos anteriores, Diamond-Square no calcula cada
    // punto de manera independiente. El algoritmo comienza con cuatro valores
    // en las esquinas y va completando progresivamente los puntos faltantes.
    //
    // La resolución final debe cumplir:
    //
    //      resolution = 2^iterations + 1
    //
    // Por ejemplo:
    //
    //      iterations = 5  -> resolution = 33
    //      iterations = 6  -> resolution = 65
    //      iterations = 7  -> resolution = 129
    //
    //
    // -------------------------------------------------------------------------
    // ESCALA DE TRABAJO
    // -------------------------------------------------------------------------
    //
    // stepSize representa el tamaño de las regiones que se están procesando
    // durante una iteración.
    //
    // Al comenzar:
    //
    //      stepSize = resolution - 1
    //
    // Por ejemplo, para una resolución de 9:
    //
    //      stepSize = 8
    //
    // Inicialmente solo conocemos las cuatro esquinas:
    //
    //      O-------------------------------O
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      |                               |
    //      O-------------------------------O
    //
    // Después de ejecutar Diamond Step y Square Step se reduce:
    //
    //      stepSize = stepSize / 2
    //
    // Por lo tanto:
    //
    //      8 -> 4 -> 2 -> 1
    //
    // Cada iteración trabaja así sobre regiones más pequeñas del terreno.
    //
    //
    // -------------------------------------------------------------------------
    // ROUGHNESS
    // -------------------------------------------------------------------------
    //
    // Las nuevas alturas no corresponden solamente al promedio de sus vecinos.
    // También se añade una perturbación aleatoria:
    //
    //      nuevaAltura = promedio + perturbación
    //
    // donde la perturbación pertenece aproximadamente al intervalo:
    //
    //      [-roughness, +roughness]
    //
    // En cada nueva escala se reduce su magnitud mediante:
    //
    //      currentRoughness =
    //          currentRoughness * roughnessDecay
    //
    // Ejemplo:
    //
    //      roughness = 0.40
    //      roughnessDecay = 0.50
    //
    //      Iteración 1 -> +/- 0.40
    //      Iteración 2 -> +/- 0.20
    //      Iteración 3 -> +/- 0.10
    //      Iteración 4 -> +/- 0.05
    //
    // De esta forma, las primeras iteraciones generan características de gran
    // escala y las posteriores añaden detalles cada vez más pequeños.
    //
    //
    // -------------------------------------------------------------------------
    // PROCESO GENERAL
    // -------------------------------------------------------------------------
    //
    // En cada iteración:
    //
    //      1. Ejecutar Diamond Step.
    //      2. Ejecutar Square Step.
    //      3. Reducir stepSize a la mitad.
    //      4. Reducir currentRoughness utilizando roughnessDecay.
    //
    // El proceso termina cuando stepSize llega a 1.
    //
    public static float[,] GenerateHeightmap(
        int iterations,
        int seed,
        float roughness,
        float roughnessDecay)
    {
        //Tamaño de la grilla, se le suma 1 para que la resolusion sea impar
        //y así haya una grilla central
        int resolution = (1 << iterations) + 1;

        float[,] heights = new float[resolution, resolution];
        System.Random random = new System.Random(seed);

        // Las cuatro esquinas iniciales se entregan implementadas.
        heights[0, 0] = (float)random.NextDouble();
        heights[0, resolution - 1] = (float)random.NextDouble();
        heights[resolution - 1, 0] = (float)random.NextDouble();
        heights[resolution - 1, resolution - 1] = (float)random.NextDouble();

        //tamaño de la region que vamos a afectar
        int stepSize = resolution - 1;
        //que tan brusco sera el cambio de altura en un punto
        float currentRoughness = roughness;

        while (stepSize > 1) //ejecutaremos el algoritmo mientras el tamaño sea mayor a 1
        {
            // TODO: PROCESO ITERATIVO
            //
            // 1. Ejecutar DiamondStep utilizando:
            //      heights, stepSize, currentRoughness y random.
            //
            // 2. Ejecutar SquareStep utilizando los mismos parámetros.
            //
            // Después de ambos pasos se cambia a una escala menor.

            //primero se aplica diamond step
            DiamondStep(heights, stepSize, currentRoughness, random);
            //segundo se aplica el square step
            SquareStep(heights, stepSize, currentRoughness, random);

            //Al final de cada ciclo el tamaño se divide por dos
            stepSize /= 2;
            //la rugosidad tambien se va disminuyendo
            currentRoughness *= roughnessDecay;
        }

        return heights;
    }

    // -------------------------------------------------------------------------
    // DIAMOND STEP
    // -------------------------------------------------------------------------
    //
    // Objetivo:
    // Calcular el punto central de cada cuadrado utilizando sus cuatro esquinas.
    //
    // Para un cuadrado:
    //
    //      A-----------------------B
    //      |                       |
    //      |                       |
    //      |           X           |
    //      |                       |
    //      |                       |
    //      C-----------------------D
    //
    // el nuevo punto X se calcula utilizando:
    //
    //      promedio = (A + B + C + D) / 4
    //
    // y posteriormente:
    //
    //      X = promedio + RandomOffset(random, roughness)
    //
    //
    // -------------------------------------------------------------------------
    // halfStep
    // -------------------------------------------------------------------------
    //
    // halfStep corresponde a la mitad del tamaño de la región actual:
    //
    //      halfStep = stepSize / 2
    //
    // Si:
    //
    //      stepSize = 8
    //
    // entonces:
    //
    //      halfStep = 4
    //
    // Si la esquina superior izquierda de una región se encuentra en (0,0),
    // su centro estará entonces en:
    //
    //      (4,4)
    //
    // Visualmente:
    //
    //      (0,0) ---------------------- (8,0)
    //        O---------------------------O
    //        |                           |
    //        |                           |
    //        |             X             |
    //        |           (4,4)           |
    //        |                           |
    //        O---------------------------O
    //      (0,8) ---------------------- (8,8)
    //
    //
    // -------------------------------------------------------------------------
    // RECORRIDO
    // -------------------------------------------------------------------------
    //
    // En una iteración pueden existir varios cuadrados.
    //
    // Por ejemplo:
    //
    //      O-----------O-----------O
    //      |           |           |
    //      |     X     |     X     |
    //      |           |           |
    //      O-----------O-----------O
    //      |           |           |
    //      |     X     |     X     |
    //      |           |           |
    //      O-----------O-----------O
    //
    // El recorrido debe saltar de un centro al siguiente utilizando stepSize.
    //
    // Para cada centro:
    //
    //      topLeft     -> y - halfStep, x - halfStep
    //      topRight    -> y - halfStep, x + halfStep
    //      bottomLeft  -> y + halfStep, x - halfStep
    //      bottomRight -> y + halfStep, x + halfStep
    //
    // Finalmente, la nueva altura debe mantenerse en el rango [0,1].
    //
    private static void DiamondStep(
        float[,] heights,
        int stepSize,
        float roughness,
        System.Random random)
    {
        int resolution = heights.GetLength(0);
        //la mitad del tamaño de los cuadrados
        int halfStep = stepSize / 2;

        // TODO: DIAMOND STEP
        //
        // 1. Recorrer los centros de cada cuadrado.
        // 2. Obtener las cuatro esquinas utilizando halfStep.
        // 3. Calcular el promedio de las cuatro alturas.
        // 4. Añadir RandomOffset(random, roughness).
        // 5. Mantener la nueva altura dentro del rango [0,1].
        // 6. Guardar el resultado en el punto central.

        //iteramos atravez de los cuadrados
        //empezamos en halfstep ya que asi nos paramos en el centro de los cuadrados
        //para ir de centro en centro sumamos el tamaño (stepsize) luego de cada ciclo
        for(int y = halfStep; y < resolution; y += stepSize)
        {
            for(int x = halfStep; x < resolution; x += stepSize)
            {
                //obtenemos las cuatro esquinas
                float topLeft = heights[y - halfStep, x - halfStep];
                float topRight = heights[y - halfStep, x + halfStep];
                float bottomLeft = heights[y + halfStep, x - halfStep];
                float bottomRight = heights[y + halfStep, x + halfStep];

                //calculamos el promedio de las alturas de las cuatro esquinas
                float promedio = (topRight + topLeft + bottomLeft + bottomRight) / 4.0f;

                //sumamos un desface aleatorio usando random offset
                float pointX = promedio + RandomOffset(random, roughness);

                //usamos clamp para mantener la altura entre 0 y 1
                pointX = Mathf.Clamp(pointX, 0f, 1f);
                
                //guardamos la altura en el punto central
                heights[y, x] = pointX;
            }
        }
    }

    // -------------------------------------------------------------------------
    // SQUARE STEP
    // -------------------------------------------------------------------------
    //
    // Objetivo:
    // Completar los puntos restantes utilizando los valores generados durante
    // Diamond Step y los valores ya existentes.
    //
    // Después de Diamond Step tenemos conceptualmente:
    //
    //      O-----------------------O
    //      |                       |
    //      |                       |
    //      |           X           |
    //      |                       |
    //      |                       |
    //      O-----------------------O
    //
    // Square Step calcula los puntos intermedios de los lados:
    //
    //      O-----------S-----------O
    //      |                       |
    //      |                       |
    //      S           X           S
    //      |                       |
    //      |                       |
    //      O-----------S-----------O
    //
    // Cada punto S utiliza los vecinos existentes en las cuatro direcciones:
    //
    //                  arriba
    //                    O
    //                    |
    //                    |
    //      izquierda O---S---O derecha
    //                    |
    //                    |
    //                    O
    //                  abajo
    //
    // Su valor corresponde a:
    //
    //      promedio de vecinos válidos
    //                  +
    //      RandomOffset(random, roughness)
    //
    //
    // -------------------------------------------------------------------------
    // PUNTOS DE BORDE
    // -------------------------------------------------------------------------
    //
    // No todos los puntos poseen cuatro vecinos.
    //
    // Por ejemplo, un punto ubicado en el borde superior:
    //
    //             borde del terreno
    //
    //          O-------S-------O
    //                  |
    //                  |
    //                  O
    //
    // no posee un vecino hacia arriba.
    //
    // Por lo tanto, se deben considerar únicamente los vecinos cuya posición
    // se encuentre dentro de la matriz.
    //
    // En lugar de dividir siempre por 4:
    //
    //      promedio = suma / cantidadDeVecinosValidos
    //
    // Un punto interior normalmente tendrá 4 vecinos.
    // Un punto del borde normalmente tendrá 3.
    //
    //
    // -------------------------------------------------------------------------
    // RECORRIDO ALTERNADO
    // -------------------------------------------------------------------------
    //
    // El Square Step no recorre una grilla rectangular normal. Las posiciones
    // que deben calcularse están desplazadas en filas alternadas.
    //
    // Después del Diamond Step, el patrón que buscamos es:
    //
    //          S       S       S
    //
    //      S       S       S
    //
    //          S       S       S
    //
    //      S       S       S
    //
    // Es decir, una fila comienza desplazada respecto de la siguiente.
    //
    // Visualmente:
    //
    //      O-----S-----O-----S-----O
    //      |           |           |
    //      S     X     S     X     S
    //      |           |           |
    //      O-----S-----O-----S-----O
    //      |           |           |
    //      S     X     S     X     S
    //      |           |           |
    //      O-----S-----O-----S-----O
    //
    // Por esto, para cada fila se debe determinar si el primer punto comienza
    // en:
    //
    //      x = halfStep
    //
    // o en:
    //
    //      x = 0
    //
    // dependiendo de la fila actual.
    //
    // Posteriormente los puntos de una misma fila se encuentran separados por
    // stepSize.
    //
    // Finalmente, cada nueva altura debe mantenerse en [0,1].
    //
    private static void SquareStep(
        float[,] heights,
        int stepSize,
        float roughness,
        System.Random random)
    {
        int resolution = heights.GetLength(0);
        int halfStep = stepSize / 2;

        // TODO: SQUARE STEP
        //
        // 1. Recorrer las filas separadas por halfStep.
        // 2. Determinar si la fila comienza en x = 0 o x = halfStep.
        // 3. Recorrer los puntos de esa fila utilizando stepSize.
        // 4. Para cada punto:
        //      - Inicializar suma y contador.
        //      - Comprobar vecino izquierdo.
        //      - Comprobar vecino derecho.
        //      - Comprobar vecino superior.
        //      - Comprobar vecino inferior.
        // 5. Calcular el promedio utilizando solo los vecinos válidos.
        // 6. Añadir RandomOffset(random, roughness).
        // 7. Mantener el resultado dentro de [0,1].
        // 8. Guardar la nueva altura.

        //luego del diamond step tenemos un punto en el centro
        //ahora tenemos que encontrar los puntos intermedios de los lados del cuadrado
        //esto en la grilla creara un patron de filas alternadas
        for(int y = 0; y < resolution; y += halfStep) //empezamos el ciclo normalmente en y
        {
            //pero debemos determinar si nuestra fila alternada (que esta en el eje x) comienza
            //en 0 io halfstep
            int startX;

            //para eso comprobamos si nuestro punto y es divisible por stepsize, si lo es sera halfstep
            if(y % stepSize == 0)
            {
                startX = halfStep;
            }
            else //de lo contrario sera 0
            {
                startX = 0;
            }

            //determinado donde comienza la fila hacemos el segundo ciclo
            for(int x = startX; x < resolution; x += stepSize)
            {
                //ya que nuestro punto ahora puede tener 4 o 3 vecinos dependiendo si esta en un borde o en
                //el centro del cuadrado, debemos contarlos, para eso usaremos la variable counter.
                int counter = 0;
                //suma acumulara la suma de las alturas de los vecinos
                float suma = 0f;

                //si mi vecino al lado izquierdo es mayor a 0 existe
                if (x - halfStep >= 0)
                {
                    suma += heights[y, x - halfStep];
                    counter++;
                }

                //si mi vecino a mi lado derecho es menor que la resolusion de la grilla existe
                if (x + halfStep < resolution)
                {
                    suma += heights[y, x + halfStep];
                    counter++;
                }

                //si mi vecino arriba es mayor o igual a 0 existe
                if (y - halfStep >= 0)
                {
                    suma += heights[y - halfStep, x];
                    counter++;
                }

                //si mi vecino abajo es menor que la resolusion de la grilla existe
                if (y + halfStep < resolution)
                {
                    suma += heights[y + halfStep, x];
                    counter++;
                }

                //ahora calculamos el promedio 
                float promedio = suma / counter;

                //le sumamos desface aleatorio
                promedio += RandomOffset(random, roughness);

                //lo mantenemos entre 0 y 1 
                promedio = Mathf.Clamp(promedio, 0f, 1f);

                //guardamos el valor de la nueva altura en la grilla
                heights[y, x] = promedio;
            }
        }
    }

    // -------------------------------------------------------------------------
    // PERTURBACIÓN ALEATORIA
    // -------------------------------------------------------------------------
    //
    // Este método se entrega implementado.
    //
    // Su objetivo es generar la perturbación que se añade al promedio de los
    // vecinos durante Diamond Step y Square Step.
    //
    // Devuelve un valor dentro del intervalo:
    //
    //      [-magnitude, +magnitude]
    //
    // Por ejemplo:
    //
    //      magnitude = 0.4
    //
    // puede producir valores entre:
    //
    //      -0.4 y +0.4
    //
    // Durante las iteraciones posteriores, magnitude disminuye debido a
    // roughnessDecay, produciendo variaciones progresivamente menores.
    private static float RandomOffset(System.Random random, float magnitude)
    {
        float randomValue = (float)random.NextDouble();

        return Mathf.Lerp(-magnitude, magnitude, randomValue);
    }
}