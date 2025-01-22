using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Units
{
    public class CaptureAbility : Ability
    {
        private Unit capturingStructure;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (CanPerform(cellGrid))
            {
                capturingStructure = GetComponent<Unit>().Cell.CurrentUnits
                    .Select(u => u.GetComponent<CapturableAbility>())
                    .OfType<CapturableAbility>()
                    .FirstOrDefault()
                    ?.GetComponent<Unit>();

                if (capturingStructure != null)
                {
                    capturingStructure.GetComponent<CapturableAbility>().Capture(GetComponent<Unit>().PlayerNumber);
                    UnitReference.ActionPoints -= 1;
                }
            }

            yield return null;
        }

        public override void OnTurnStart(CellGrid cellGrid)
        {
            if (capturingStructure != null && UnitReference.Cell.CurrentUnits.Contains(capturingStructure))
            {
                capturingStructure.GetComponent<CapturableAbility>().Capture(UnitReference.PlayerNumber);
                capturingStructure = null;
            }
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            var capturable = GetComponent<Unit>().Cell.CurrentUnits
                .Select(u => u.GetComponent<CapturableAbility>())
                .OfType<CapturableAbility>()
                .ToList();

            return capturable.Count > 0 && capturable[0].GetComponent<Unit>().PlayerNumber != GetComponent<Unit>().PlayerNumber && UnitReference.ActionPoints > 0;
        }

        public override IDictionary<string, string> Encapsulate()
        {
            return new Dictionary<string, string>();
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked)
        {
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }
    }
}
