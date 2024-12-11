using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using UnityEngine;

namespace TbsFramework.Units.Abilities
{
    public class AttackAbility : Ability
    {
        public Unit UnitToAttack { get; set; }
        public int UnitToAttackID { get; set; }

        List<Unit> inAttackRange;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (CanPerform(cellGrid) && UnitReference.IsUnitAttackable(UnitToAttack, UnitReference.Cell))
            {
                UnitReference.AttackHandler(UnitToAttack);
                yield return new WaitForSeconds(0.5f);
            }
            yield return null;
        }
        public override void Display(CellGrid cellGrid)
        {
            var enemyUnits = cellGrid.GetEnemyUnits(cellGrid.CurrentPlayer);
            inAttackRange = enemyUnits.Where(u => UnitReference.IsUnitAttackable(u, UnitReference.Cell)).ToList();
            inAttackRange.ForEach(u => u.MarkAsReachableEnemy());
        }

        public override void OnUnitClicked(Unit unit, CellGrid cellGrid)
        {
            if (UnitReference.IsUnitAttackable(unit, UnitReference.Cell))
            {
                UnitToAttack = unit;
                UnitToAttackID = UnitToAttack.UnitID;
                StartCoroutine(HumanExecute(cellGrid));
            }
            else if (cellGrid.GetCurrentPlayerUnits().Contains(unit))
            {
                cellGrid.cellGridState = new CellGridStateAbilitySelected(cellGrid, unit, unit.GetComponents<Ability>().ToList());
            }
        }

        public override void OnCellClicked(Cell cell, CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid);
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            inAttackRange.ForEach(u =>
            {
                if (u != null)
                {
                    u.UnMark();
                }
            });
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints <= 0)
            {
                return false;
            }

            var enemyUnits = cellGrid.GetEnemyUnits(cellGrid.CurrentPlayer);
            inAttackRange = enemyUnits.Where(u => UnitReference.IsUnitAttackable(u, UnitReference.Cell)).ToList();

            return inAttackRange.Count > 0;
        }

        public override IDictionary<string, string> Encapsulate()
        {
            Dictionary<string, string> actionParameters = new Dictionary<string, string>();
            actionParameters.Add("target_id", UnitToAttackID.ToString());

            return actionParameters;
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked = false)
        {
            var targetID = int.Parse(actionParams["target_id"]);
            var target = cellGrid.Units.Find(u => u.UnitID == targetID);

            UnitToAttack = target;
            UnitToAttackID = targetID;
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }
    }
}

