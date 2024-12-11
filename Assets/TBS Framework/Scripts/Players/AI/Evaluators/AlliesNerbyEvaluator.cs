using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;

namespace TbsFramework.Players.AI.Evaluators
{
    public class AlliesNerbyEvaluator : CellEvaluator
    {
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var neighbours = cellToEvaluate.GetNeighbours(cellGrid.Cells);
            var nAlliesNearby = 0f;

            for (int i = 0; i < neighbours.Count; i++)
            {
                Cell c = neighbours[i];
                if (c.CurrentUnits.Count > 0)
                {
                    nAlliesNearby += c.CurrentUnits[0].PlayerNumber == evaluatingUnit.PlayerNumber && c.CurrentUnits[0] != evaluatingUnit ? 1 : 0;
                }
            }

            return nAlliesNearby / neighbours.Count;
        }
    }
}