using TbsFramework.Cells;
using TbsFramework.Units;

namespace TbsFramework.Grid
{
    public partial class CellGrid
    {
        public abstract class CellGridState
        {
            protected CellGrid _cellGrid;

            protected CellGridState(CellGrid cellGrid)
            {
                _cellGrid = cellGrid;
            }

            /// <summary>
            /// Initiates a transition to the specified next state of the CellGrid.
            /// </summary>
            /// <param name="nextState">The next state to transition to.</param>
            /// <returns>The next CellGridState after the transition.</returns>
            public virtual CellGridState MakeTransition(CellGridState nextState)
            {
                return nextState;
            }

            /// <summary>
            /// Method is called when a unit is clicked on.
            /// </summary>
            /// <param name="unit">Unit that was clicked.</param>
            public virtual void OnUnitClicked(Unit unit)
            {
            }

            /// <summary>
            /// Method is called when a unit is highlighted (cursor over the unit).
            /// </summary>
            /// <param name="unit">Unit that was highlighted.</param>
            public virtual void OnUnitHighlighted(Unit unit)
            {
            }

            /// <summary>
            /// Method is called when a unit is no longer highlighted.
            /// </summary>
            /// <param name="unit">Unit that was de-highlighted.</param>
            public virtual void OnUnitDehighlighted(Unit unit)
            {
            }

            /// <summary>
            /// Method is called when mouse exits cell's collider.
            /// </summary>
            /// <param name="cell">Cell that was deselected.</param>
            public virtual void OnCellDeselected(Cell cell)
            {
                cell.UnMark();
            }

            /// <summary>
            /// Method is called when mouse enters cell's collider.
            /// </summary>
            /// <param name="cell">Cell that was selected.</param>
            public virtual void OnCellSelected(Cell cell)
            {
                cell.MarkAsHighlighted();
            }

            /// <summary>
            /// Method is called when a cell is clicked.
            /// </summary>
            /// <param name="cell">Cell that was clicked.</param>
            public virtual void OnCellClicked(Cell cell)
            {
            }

            /// <summary>
            /// Triggers ending the turn.
            /// </summary>
            /// <param name="isNetworkInvoked">Indicates if the turn end is invoked by a remote action</param>
            public virtual void EndTurn(bool isNetworkInvoked)
            {
                _cellGrid.EndTurnExecute(isNetworkInvoked);
            }

            /// <summary>
            /// Method is called on transitioning into a state.
            /// </summary>
            public virtual void OnStateEnter()
            {
                foreach (var cell in _cellGrid.Cells)
                {
                    cell.UnMark();
                }
            }

            /// <summary>
            /// Method is called on transitioning out of a state.
            /// </summary>
            public virtual void OnStateExit()
            {
            }
        }
    }
}