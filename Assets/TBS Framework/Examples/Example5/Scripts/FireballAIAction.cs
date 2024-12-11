using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public class FireballAIAction : AIAction
    {
        public FireballSpell Spell;
        public float Probability = 0.5f;

        private List<(Cell cell, int value)> cellScores;

        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            Spell.OnAbilitySelected(cellGrid);
        }
        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (Spell == null)
            {
                return false;
            }

            return Spell.ManaCost <= unit.GetComponent<SpellCastingAbility>().CurrentMana && Random.Range(0f, 1f) <= Probability;
        }
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            cellScores = cellGrid.Cells.Select(c =>
            {
                return (cell: c, value: cellGrid.Cells.Where(c2 => c.GetDistance(c2) <= Spell.Range)
                                                      .Select(cInRange => cInRange.CurrentUnits.Count == 0 ? 0 : cInRange.CurrentUnits[0].PlayerNumber == player.PlayerNumber ? -1 * Mathf.Min(Spell.Damage, cInRange.CurrentUnits[0].HitPoints) : 1 * Mathf.Min(Spell.Damage, cInRange.CurrentUnits[0].HitPoints))
                                                      .Aggregate((result, next) => result + next));
            }).OrderByDescending(c => c.value)
            .ToList();
        }
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            Spell.SelectedCell = cellScores.First().cell;
            yield return StartCoroutine(Spell.AIExecute(cellGrid));
            yield return new WaitForSeconds(1);
        }
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            Dictionary<Cell, DebugInfo> cellDebugInfo = new Dictionary<Cell, DebugInfo>();
            for (int i = 0; i < cellScores.Count; i++)
            {
                var (cell, value) = cellScores[i];
                var valueNormalized = ((float)value / cellScores.First().value + 1) * 0.5f;
                var color = i == 0 ? Color.blue : Color.Lerp(Color.red, Color.green, valueNormalized);

                cellDebugInfo[cell] = new DebugInfo(valueNormalized.ToString(), color);
            }
            (cellGrid.cellGridState as CellGridStateAITurn).CellDebugInfo = cellDebugInfo;
        }
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            if (cellScores != null)
            {
                foreach (var (cell, _) in cellScores)
                {
                    cell.UnMark();
                }
            }
        }
    }
}

