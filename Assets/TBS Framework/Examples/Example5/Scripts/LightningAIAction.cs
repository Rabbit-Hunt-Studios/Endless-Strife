using System.Collections;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public class LightningAIAction : AIAction
    {
        public LightningSpell Spell;
        public float Probability = 0.5f;

        private Unit target;

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
            target = cellGrid.GetEnemyUnits(player).OrderBy(u => u.HitPoints).Skip(1).First();
        }
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            Spell.SelectedTarget = target;
            Spell.SelectedTargetID = target.UnitID;
            yield return StartCoroutine(Spell.AIExecute(cellGrid));
            yield return new WaitForSeconds(1);
        }
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
        }
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
        }
    }
}