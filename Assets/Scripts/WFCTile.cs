using System;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public enum WFCDirection { North, East, South, West }

    [Serializable]
    public class WFCTile
    {
        [Header("Datos del patron")]
        public string id;
        public Sprite sprite;
        [Min(0.01f)] public float weight = 1f;
        public bool available = true;

        [Header("Conexiones (true = abierto)")]
        public bool north, east, south, west;

        // Consulta si esta pieza tiene una abertura en una direccion.
        public bool HasOpening(WFCDirection direction)
        {
            switch (direction)
            {
                case WFCDirection.North: return north;
                case WFCDirection.East: return east;
                case WFCDirection.South: return south;
                case WFCDirection.West: return west;
                default: return false;
            }
        }

        /*
         * COMPATIBILIDAD ENTRE PIEZAS
         *
         * Dos piezas pueden ser vecinas cuando sus conexiones
         * enfrentadas son compatibles.
         *
         * Ejemplo:
         *
         * Pieza A: East = true
         * Pieza B: West = true
         *
         * Ambas pueden conectarse horizontalmente.
         *
         * Si A.East = true y B.West = false, la conexion
         * no es valida.
         *
         * Las conexiones cerradas tambien deben coincidir.
         * La direccion indica donde se ubica B respecto de A.
         */

        public bool IsCompatible(WFCTile neighbor, WFCDirection direction)
        {
            // TODO:
            // Identificar las conexiones enfrentadas y
            // determinar si ambas piezas pueden ser vecinas.

            if (neighbor == null) return false;

            switch (direction)
            {
                case WFCDirection.North: return north == neighbor.south;
                case WFCDirection.East: return east == neighbor.west;
                case WFCDirection.South: return south == neighbor.north;
                case WFCDirection.West: return west == neighbor.east;
            }

            return false;
        }
    }
}
