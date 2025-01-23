using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Units
{
    public class DisableColliderAbility : Ability
    {
        IEnumerable<SpawnAbility> playerBaseInMovementRange;
        
        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            var playerBases = FindObjectsOfType<SpawnAbility>();
            playerBaseInMovementRange = playerBases.Where(f => UnitReference.GetComponent<MoveAbility>().availableDestinations.Contains(f.UnitReference.Cell));
            foreach (var playerBase in playerBaseInMovementRange)
            {
                playerBase.GetComponent<Collider2D>().enabled = false;
            }
        }

        public override void OnAbilityDeselected(CellGrid cellGrid)
        {
            foreach (var playerBase in playerBaseInMovementRange)
            {
                playerBase.GetComponent<Collider2D>().enabled = true;
            }
        }
    }
}

