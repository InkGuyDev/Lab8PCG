using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    /*
     * TILESET
     *
     * Un TileSet contiene los patrones disponibles para generar
     * contenido: sprites, conexiones, pesos y disponibilidad.
     *
     * Cada conjunto puede tener una cantidad diferente de piezas.
     * Por ejemplo, caminos de una mina o segmentos de una ciudad.
     *
     * Las piezas se configuran manualmente desde Unity.
     * No se requiere construirlas mediante scripts de Editor.
     */

    [Serializable]
    public class WFCTileSet
    {
        [Header("Identificacion")]
        public string id = "TileSet";

        [Header("Patrones disponibles")]
        [SerializeField] private List<WFCTile> tiles = new List<WFCTile>();

        public IReadOnlyList<WFCTile> Tiles => tiles;
    }
}
