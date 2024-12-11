using System.Collections.Generic;
using UnityEngine;

namespace TbsFramework.Cells
{
    /// <summary>
    /// Implementation of hexagonal cell.
    /// </summary>
    public abstract class Hexagon : Cell
    {
        List<Cell> neighbours = null;
        /// <summary>
        /// HexGrids comes in four types regarding the layout. 
        /// This distinction is necessary to convert cube coordinates to offset and vice versa.
        /// </summary>
        [HideInInspector]
        public HexGridType HexGridType;

        protected Vector3 CubeCoord
        {
            get
            {
                return OffsetToCubeCoords(OffsetCoord, HexGridType);
            }
        }

        /// <summary>
        /// Converts offset coordinates into cube coordinates.
        /// Cube coordinates is another system of coordinates that makes calculation on hex grids easier.
        /// </summary>
        protected Vector3 OffsetToCubeCoords(Vector2 offsetCoords, HexGridType gridType)
        {
            Vector3 cubeCoords = new Vector3();
            switch (gridType)
            {
                case HexGridType.odd_q:
                    {
                        cubeCoords.x = offsetCoords.x;
                        cubeCoords.z = offsetCoords.y - (offsetCoords.x + (Mathf.Abs(offsetCoords.x) % 2)) / 2;
                        cubeCoords.y = -cubeCoords.x - cubeCoords.z;
                        break;
                    }
                case HexGridType.even_q:
                    {
                        cubeCoords.x = offsetCoords.x;
                        cubeCoords.z = offsetCoords.y - (offsetCoords.x - (Mathf.Abs(offsetCoords.x) % 2)) / 2;
                        cubeCoords.y = -cubeCoords.x - cubeCoords.z;
                        break;
                    }
                case HexGridType.odd_r:
                    {
                        cubeCoords.x = offsetCoords.x - (offsetCoords.y - (Mathf.Abs(offsetCoords.y) % 2)) / 2;
                        cubeCoords.z = offsetCoords.y;
                        cubeCoords.y = -cubeCoords.x - cubeCoords.z;
                        break;
                    }
                case HexGridType.even_r:
                    {
                        cubeCoords.x = offsetCoords.x - (offsetCoords.y + (Mathf.Abs(offsetCoords.y) % 2)) / 2;
                        cubeCoords.z = offsetCoords.y;
                        cubeCoords.y = -cubeCoords.x - cubeCoords.z;
                        break;
                    }
            }
            return cubeCoords;
        }
        /// <summary>
        /// Converts cube coordinates back to offset coordinates.
        /// </summary>
        /// <param name="cubeCoords">Cube coordinates to convert.</param>
        /// <returns>Offset coordinates corresponding to given cube coordinates.</returns>
        protected Vector2 CubeToOffsetCoords(Vector3 cubeCoords)
        {
            Vector2 offsetCoords = new Vector2();

            switch (HexGridType)
            {
                case HexGridType.odd_q:
                    {
                        offsetCoords.x = cubeCoords.x;
                        offsetCoords.y = cubeCoords.z + (cubeCoords.x + (Mathf.Abs(cubeCoords.x) % 2)) / 2;
                        break;
                    }
                case HexGridType.even_q:
                    {
                        offsetCoords.x = cubeCoords.x;
                        offsetCoords.y = cubeCoords.z + (cubeCoords.x - (Mathf.Abs(cubeCoords.x) % 2)) / 2;
                        break;
                    }
                case HexGridType.odd_r:
                    {
                        offsetCoords.x = cubeCoords.x + (cubeCoords.z - (Mathf.Abs(cubeCoords.z) % 2)) / 2;
                        offsetCoords.y = cubeCoords.z;
                        break;
                    }
                case HexGridType.even_r:
                    {
                        offsetCoords.x = cubeCoords.x + (cubeCoords.z + (Mathf.Abs(cubeCoords.z) % 2)) / 2;
                        offsetCoords.y = cubeCoords.z;
                        break;
                    }
            }
            return offsetCoords;
        }

        protected static readonly Vector3[] _directions =  {
        new Vector3(+1, -1, 0), new Vector3(+1, 0, -1), new Vector3(0, +1, -1),
        new Vector3(-1, +1, 0), new Vector3(-1, 0, +1), new Vector3(0, -1, +1)};

        public override int GetDistance(Cell other)
        {
            var _other = other as Hexagon;

            var cubeCoords = CubeCoord;
            var otherCubeCoords = _other.CubeCoord;

            int distance = (int)(Mathf.Abs(cubeCoords.x - otherCubeCoords.x) + Mathf.Abs(cubeCoords.y - otherCubeCoords.y) + Mathf.Abs(cubeCoords.z - otherCubeCoords.z)) / 2;
            return distance;
        }//Distance is given using Manhattan Norm.
        public override List<Cell> GetNeighbours(List<Cell> cells)
        {
            if (neighbours == null)
            {
                neighbours = new List<Cell>(6);
                foreach (var direction in _directions)
                {
                    var neighbour = cells.Find(c => c.OffsetCoord == CubeToOffsetCoords(CubeCoord + direction));
                    if (neighbour == null) continue;
                    neighbours.Add(neighbour);
                }
            }
            return neighbours;

        }//Each hex cell has six neighbors, which positions on grid relative to the cell are stored in _directions constant.
        public override void CopyFields(Cell newCell)
        {
            newCell.OffsetCoord = OffsetCoord;
            (newCell as Hexagon).HexGridType = HexGridType;
        }
    }

    public enum HexGridType
    {
        even_q,
        odd_q,
        even_r,
        odd_r
    };
}