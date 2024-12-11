using System.Collections.Generic;
using TbsFramework.Cells;
using UnityEditor;
using UnityEngine;

namespace TbsFramework.EditorUtils.GridGenerators
{
    /// <summary>
    /// Generates hexagonal shaped grid of hexagons.
    /// </summary>
    [ExecuteInEditMode()]
    class HexagonalHexGridGenerator : ICellGridGenerator
    {
#pragma warning disable 0649
        public GameObject HexagonPrefab;
        public int Radius;
#pragma warning restore 0649

        public override GridInfo GenerateGrid()
        {
            var hexagons = new List<Cell>();

            if (HexagonPrefab.GetComponent<Hexagon>() == null)
            {
                Debug.LogError("Invalid hexagon prefab provided");
                return null;
            }

            var hexSize = HexagonPrefab.GetComponent<Cell>().GetCellDimensions();

            for (int i = 0; i < Radius; i++)
            {
                for (int j = 0; j < (Radius * 2) - i - 1; j++)
                {
                    var hexagon = PrefabUtility.InstantiatePrefab(HexagonPrefab) as GameObject;
                    var position = Is2D ? new Vector3((i * hexSize.x * 0.75f), (i * hexSize.y * 0.5f) + (j * hexSize.y)) :
                        new Vector3((i * hexSize.x * 0.75f), 0, (i * hexSize.z * 0.5f) + (j * hexSize.z));

                    hexagon.transform.position = position;
                    hexagon.GetComponent<Hexagon>().OffsetCoord = new Vector2(i, Radius - j - 1 - (i / 2));
                    hexagon.GetComponent<Hexagon>().HexGridType = HexGridType.odd_q;
                    hexagon.GetComponent<Hexagon>().MovementCost = 1;
                    hexagons.Add(hexagon.GetComponent<Cell>());

                    hexagon.transform.parent = CellsParent;

                    if (i == 0) continue;

                    var hexagon2 = PrefabUtility.InstantiatePrefab(HexagonPrefab) as GameObject;
                    position = Is2D ? new Vector3((-i * hexSize.x * 0.75f), (i * hexSize.y * 0.5f) + (j * hexSize.y)) :
                        new Vector3((-i * hexSize.x * 0.75f), 0, (i * hexSize.z * 0.5f) + (j * hexSize.z));

                    hexagon2.transform.position = position;
                    hexagon2.GetComponent<Hexagon>().OffsetCoord = new Vector2(-i, Radius - j - 1 - (i / 2));
                    hexagon2.GetComponent<Hexagon>().HexGridType = HexGridType.odd_q;
                    hexagon2.GetComponent<Hexagon>().MovementCost = 1;
                    hexagons.Add(hexagon2.GetComponent<Cell>());

                    hexagon2.transform.parent = CellsParent;
                }
            }

            return GetGridInfo(hexagons);
        }
    }
}