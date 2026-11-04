using System;
using UnityEngine;

namespace Laboratorios.Lab10.WaveFunctionCollapse
{
    /*
     * LABORATORIO 8 - WFC II: PATRONES DE ENTRENAMIENTO
     *
     * Cada tile se identifica mediante un simbolo (por ejemplo P, R, A o B).
     * El prefab representa ese tipo de terreno durante la visualizacion.
     * La propiedad available permite excluirlo de la generacion.
     *
     * A diferencia del laboratorio anterior, aqui NO se configuran
     * manualmente las conexiones entre piezas: se aprenden desde
     * una matriz de ejemplo en WFCTileSet.
     *
     * Este script se entrega completo. No contiene TODO.
     */

    public enum WFCDirection { North, East, South, West, NorthEast, SouthEast, SouthWest, NorthWest }

    [Serializable]
    public class WFCTile
    {
        [Tooltip("Un caracter, igual al utilizado en la matriz de entrenamiento.")]
        public string id;
        public GameObject prefab;
        public bool available = true;
    }
}
