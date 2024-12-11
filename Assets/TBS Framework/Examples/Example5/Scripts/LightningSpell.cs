using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public class LightningSpell : SpellAbility
    {
        public int nJumps;
        public int Damage;
        private List<Unit> inRange;

        public Unit SelectedTarget { get; set; }
        public int SelectedTargetID { get; set; }

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (CanPerform(cellGrid))
            {
                if (inRange == null)
                {
                    inRange = new List<Unit>() { SelectedTarget };
                    var currentUnit = SelectedTarget;
                    for (var i = 0; i < nJumps; i++)
                    {
                        currentUnit = cellGrid.GetEnemyUnits(cellGrid.CurrentPlayer)
                                              .Where(u => !inRange.Contains(u) && u.Cell != null)
                                              .OrderBy(u => u.Cell.GetDistance(currentUnit.Cell))
                                              .FirstOrDefault();
                        if (currentUnit == null)
                        {
                            break;
                        }
                        inRange.Add(currentUnit);
                    }
                }

                Unit tempUnit = null;
                for (int i = 0; i < inRange.Count; i++)
                {
                    Unit unitInRange = inRange[i];
                    unitInRange.DefendHandler(UnitReference, Mathf.FloorToInt(Damage * Mathf.Pow(2, i * (-1))));

                    tempUnit = unitInRange;
                }

                UnitReference.MarkAsAttacking(tempUnit);

                yield return base.Act(cellGrid, false);
            }
        }

        public override void OnUnitHighlighted(Unit unit, CellGrid cellGrid)
        {
            if ((unit as HOMMUnit).IsHero || unit.PlayerNumber == UnitReference.PlayerNumber)
            {
                return;
            }

            inRange = new List<Unit>() { unit };
            var currentUnit = unit;
            for (var i = 0; i < nJumps; i++)
            {
                currentUnit = cellGrid.GetEnemyUnits(cellGrid.CurrentPlayer)
                                      .Where(u => !inRange.Contains(u) && u.Cell != null)
                                      .OrderBy(u => u.Cell.GetDistance(currentUnit.Cell))
                                      .FirstOrDefault();
                if (currentUnit == null)
                {
                    break;
                }
                inRange.Add(currentUnit);
            }

            foreach (var unitInRange in inRange)
            {
                unitInRange.MarkAsReachableEnemy();
            }
        }

        public override void OnUnitDehighlighted(Unit unit, CellGrid cellGrid)
        {
            if ((unit as HOMMUnit).IsHero || unit.PlayerNumber == UnitReference.PlayerNumber)
            {
                return;
            }

            foreach (var unitInRange in inRange)
            {
                unitInRange.UnMark();
            }
        }

        public override void OnUnitClicked(Unit unit, CellGrid cellGrid)
        {
            if ((unit as HOMMUnit).IsHero || unit.PlayerNumber == UnitReference.PlayerNumber)
            {
                return;
            }

            SelectedTarget = unit;

            if (CanPerform(cellGrid))
            {
                StartCoroutine(Execute(cellGrid,
                    _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                    _ => cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid)));
            }
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            base.CleanUp(cellGrid);
            if (inRange != null)
            {
                foreach (var unit in inRange)
                {
                    unit.UnMark();
                }
            }
        }

        public override string GetDetails()
        {
            return string.Format("{0} Mana\n{1} Damage", ManaCost, Damage);
        }

        public override IDictionary<string, string> Encapsulate()
        {
            Dictionary<string, string> actionParameters = new Dictionary<string, string>();
            actionParameters.Add("target_id", SelectedTargetID.ToString());

            return actionParameters;
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked)
        {
            var targetID = int.Parse(actionParams["target_id"]);
            var target = cellGrid.Units.Find(u => u.UnitID == targetID);

            SelectedTarget = target;
            SelectedTargetID = targetID;
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }
    }
}