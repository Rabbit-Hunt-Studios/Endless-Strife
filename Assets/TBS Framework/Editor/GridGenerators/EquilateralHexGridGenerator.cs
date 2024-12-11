using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using TbsFramework.Cells;

namespace TbsFramework.EditorUtils.GridGenerators
{
    /// <summary>
    /// Generates equilateral shaped grid of hexagons.
    /// </summary>
    [ExecuteInEditMode()]
    public class EquilateralHexGridGenerator : ICellGridGenerator
    {
        public GameObject HexagonPrefab;
        public int SideA;
        public int SideB;

        public override GridInfo GenerateGrid()
        {
            var hexGridType = SideA % 2 == 0 ? HexGridType.even_q : HexGridType.odd_q; ;
            var hexagons = new List<Cell>();

            if (HexagonPrefab.GetComponent<Hexagon>() == null)
            {
                Debug.LogError("Invalid hexagon prefab provided");
                return null;
            }

            var hexSize = HexagonPrefab.GetComponent<Cell>().GetCellDimensions();

            for (int i = 0; i < SideA; i++)
            {
                for (int j = 0; j < SideB; j++)
                {
                    var hexagon = PrefabUtility.InstantiatePrefab(HexagonPrefab) as GameObject;
                    var position = Is2D ? new Vector3((i * hexSize.x * 0.75f), (i * hexSize.y * 0.5f) + (j * hexSize.y), 0) : new Vector3((i * hexSize.x * 0.75f), 0, (i * hexSize.z * 0.5f) + (j * hexSize.z));
                    
                    hexagon.transform.position = position;
                    hexagon.GetComponent<Hexagon>().OffsetCoord = new Vector2(SideA - i - 1, SideB - j - 1 - (i / 2));
                    hexagon.GetComponent<Hexagon>().HexGridType = hexGridType;
                    hexagon.GetComponent<Hexagon>().MovementCost = 1;
                    hexagons.Add(hexagon.GetComponent<Cell>());

                    hexagon.transform.parent = CellsParent;
                }
            }

            return GetGridInfo(hexagons);
        }
    }
}