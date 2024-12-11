using UnityEngine;
using System.Collections.Generic;
using TbsFramework.Cells;
using UnityEditor;

namespace TbsFramework.EditorUtils.GridGenerators
{
    /// <summary>
    /// Generates rectangular shaped grid of squares.
    /// </summary>
    [ExecuteInEditMode()]
    public class RectangularSquareGridGenerator : ICellGridGenerator
    {
        public GameObject SquarePrefab;

        public int Width;
        public int Height;

        public override GridInfo GenerateGrid()
        {
            var cells = new List<Cell>();

            if (SquarePrefab.GetComponent<Square>() == null)
            {
                Debug.LogError("Invalid square cell prefab provided");
                return null;
            }

            var squareSize = SquarePrefab.GetComponent<Cell>().GetCellDimensions();

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    var square = PrefabUtility.InstantiatePrefab(SquarePrefab) as GameObject;
                    var position = Is2D ? new Vector3(i * squareSize.x, j * squareSize.y, 0) :
                        new Vector3(i * squareSize.x, 0, j * squareSize.z);

                    square.transform.position = position;
                    square.GetComponent<Cell>().OffsetCoord = new Vector2(i, j);
                    square.GetComponent<Cell>().MovementCost = 1;
                    cells.Add(square.GetComponent<Cell>());

                    square.transform.parent = CellsParent;
                }
            }

            return GetGridInfo(cells);
        }
    }
}