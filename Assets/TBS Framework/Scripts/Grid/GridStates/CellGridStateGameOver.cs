namespace TbsFramework.Grid.GridStates
{
    public class CellGridStateGameOver : CellGrid.CellGridState
    {
        public CellGridStateGameOver(CellGrid cellGrid) : base(cellGrid)
        {
        }

        public override CellGrid.CellGridState MakeTransition(CellGrid.CellGridState nextState)
        {
            return this;
        }
    }
}
