using System;
using System.Collections.Generic;
using TbsFramework.Cells.Highlighters;
using TbsFramework.Grid;
using TbsFramework.Pathfinding.DataStructs;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Cells
{
    /// <summary>
    /// Class representing a single field (cell) on the grid.
    /// </summary>
    ///
    [Serializable]
    public abstract class Cell : MonoBehaviour, IGraphNode, IEquatable<Cell>
    {
        int hash = -1;
        [SerializeField]
        private Vector2 _offsetCoord;
        /// <summary>
        /// Position of the cell on the grid.
        /// </summary>
        public Vector2 OffsetCoord { get { return _offsetCoord; } set { _offsetCoord = value; } }

        public List<CellHighlighter> MarkAsReachableFn;
        public List<CellHighlighter> MarkAsPathFn;
        public List<CellHighlighter> MarkAsHighlightedFn;
        public List<CellHighlighter> UnMarkFn;

        /// <summary>
        /// Indicates if something is occupying the cell.
        /// </summary>
        public bool IsTaken;
        /// <summary>
        /// Cost of moving through the cell.
        /// </summary>
        public float MovementCost = 1;

        public List<Unit> CurrentUnits { get; private set; } = new List<Unit>();

        /// <summary>
        /// CellClicked event is invoked when user clicks on the cell. 
        /// It requires a collider on the cell game object to work.
        /// </summary>
        public event EventHandler CellClicked;
        /// <summary>
        /// CellHighlighed event is invoked when cursor enters the cell's collider. 
        /// It requires a collider on the cell game object to work.
        /// </summary>
        public event EventHandler CellHighlighted;
        /// <summary>
        /// CellDehighlighted event is invoked when cursor exits the cell's collider. 
        /// It requires a collider on the cell game object to work.
        /// </summary>
        public event EventHandler CellDehighlighted;

        public virtual void OnMouseEnter()
        {
            if (CellHighlighted != null)
                CellHighlighted.Invoke(this, EventArgs.Empty);
        }
        public virtual void OnMouseExit()
        {
            if (CellDehighlighted != null)
                CellDehighlighted.Invoke(this, EventArgs.Empty);
        }
        public virtual void OnMouseDown()
        {
            if (CellClicked != null)
                CellClicked.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method returns distance to a cell that is given as parameter. 
        /// </summary>
        public abstract int GetDistance(Cell other);

        /// <summary>
        /// Method returns cells adjacent to current cell, from list of cells given as parameter.
        /// </summary>
        public abstract List<Cell> GetNeighbours(List<Cell> cells);
        /// <summary>
        /// Method returns cell's physical dimensions It is used in grid generators.
        /// </summary>
        public abstract Vector3 GetCellDimensions();

        /// <summary>
        ///  Method marks the cell to give user an indication that selected unit can reach it.
        /// </summary>
        public virtual void MarkAsReachable()
        {
            MarkAsReachableFn?.ForEach(o => o.Apply(this));
        }
        /// <summary>
        /// Method marks the cell as a part of a path.
        /// </summary>
        public virtual void MarkAsPath()
        {
            MarkAsPathFn?.ForEach(o => o.Apply(this));
        }
        /// <summary>
        /// Method marks the cell as highlighted. It gets called when the mouse is over the cell.
        /// </summary>
        public virtual void MarkAsHighlighted()
        {
            MarkAsHighlightedFn?.ForEach(o => o.Apply(this));
        }
        /// <summary>
        /// Method returns the cell to its base appearance.
        /// </summary>
        public virtual void UnMark()
        {
            UnMarkFn?.ForEach(o => o.Apply(this));
        }
        public virtual void SetColor(Color color) { }

        public int GetDistance(IGraphNode other)
        {
            return GetDistance(other as Cell);
        }

        public virtual bool Equals(Cell other)
        {
            return OffsetCoord == other.OffsetCoord;
        }

        public override bool Equals(object other)
        {
            Cell otherCell = other as Cell;
            return otherCell != null && OffsetCoord == otherCell.OffsetCoord;
        }

        public virtual void Initialize(CellGrid cellGrid) { }

        public override int GetHashCode()
        {
            if (hash == -1)
            {
                hash = OffsetCoord.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Method for cloning field values into a new cell. Used in Tile Painter in Grid Helper
        /// </summary>
        /// <param name="newCell">Cell to copy field values to</param>
        public abstract void CopyFields(Cell newCell);

        public override string ToString()
        {
            return OffsetCoord.ToString();
        }
    }
}