using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Example4
{
    public class DisableColliderAbility : Ability
    {
        IEnumerable<SpawnAbility> factoriesInMovementRange;
        
        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            var factories = FindObjectsOfType<SpawnAbility>();
            factoriesInMovementRange = factories.Where(f => UnitReference.GetComponent<MoveAbility>().availableDestinations.Contains(f.UnitReference.Cell));
            foreach (var factory in factoriesInMovementRange)
            {
                factory.GetComponent<Collider2D>().enabled = false;
            }
        }

        public override void OnAbilityDeselected(CellGrid cellGrid)
        {
            foreach (var factory in factoriesInMovementRange)
            {
                factory.GetComponent<Collider2D>().enabled = true;
            }
        }
    }
}

