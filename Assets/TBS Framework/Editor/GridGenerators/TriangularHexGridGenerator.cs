using UnityEngine;
using System.Collections.Generic;
using TbsFramework.Cells;
using UnityEditor;

namespace TbsFramework.EditorUtils.GridGenerators
{
    /// <summary>
    /// Generates triangle shaped grid of hexagons.
    /// </summary>
    [ExecuteInEditMode()]
    public class TriangularHexGridGenerator : ICellGridGenerator
    {
        public GameObject HexagonPrefab;
        public int Side;

        public override GridInfo GenerateGrid()
        {
            List<Cell> hexagons = new List<Cell>();

            if (HexagonPrefab.GetComponent<Hexagon>() == null)
            {
                Debug.LogError("Invalid hexagon prefab provided");
                return null;
            }

            var hexSize = HexagonPrefab.GetComponent<Cell>().GetCellDimensions();

            for (int i = 0; i < Side; i++)
            {
                for (int j = 0; j < Side - i; j++)
                {
                    var hexagon = PrefabUtility.InstantiatePrefab(HexagonPrefab) as GameObject;
                    var position = Is2D ? new Vector3((i * hexSize.x * 0.75f), (i * hexSize.y * 0.5f) + (j * hexSize.y)) :
                        new Vector3((i * hexSize.x * 0.75f), 0, (i * hexSize.z * 0.5f) + (j * hexSize.z));

                    hexagon.transform.position = position;
                    hexagon.GetComponent<Hexagon>().OffsetCoord = new Vector2(i, Side - j - 1 - (i / 2));
                    hexagon.GetComponent<Hexagon>().HexGridType = HexGridType.odd_q;
                    hexagon.GetComponent<Hexagon>().MovementCost = 1;
                    hexagons.Add(hexagon.GetComponent<Cell>());

                    hexagon.transform.parent = CellsParent;
                }
            }

            return GetGridInfo(hexagons);
        }
    }
}