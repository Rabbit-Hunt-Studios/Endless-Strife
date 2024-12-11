using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;

namespace TbsFramework.Example4
{
    public class IncomeGenerationAbility : Ability
    {
        public int Amount;

        public override void OnTurnStart(CellGrid cellGrid)
        {
            var economyController = FindObjectOfType<EconomyController>();
            economyController.UpdateValue(GetComponent<Unit>().PlayerNumber, Amount);
        }
    }
}

