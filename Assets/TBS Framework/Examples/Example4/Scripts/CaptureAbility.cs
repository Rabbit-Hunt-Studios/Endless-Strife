using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.Example4
{
    public class CaptureAbility : Ability
    {
        public Button ActivationButton;
        private Unit CapturingStructure;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (CanPerform(cellGrid))
            {
                var capturable = GetComponent<Unit>().Cell.CurrentUnits.Select(u => u.GetComponent<CapturableAbility>())
                                                                  .OfType<CapturableAbility>()
                                                                  .ToList();

                var captureAmount = (int)Mathf.Ceil(GetComponent<Unit>().HitPoints * 10f / GetComponent<Unit>().TotalHitPoints);

                CapturingStructure = capturable[0].GetComponent<Unit>();
                capturable[0].Capture(captureAmount, GetComponent<Unit>().PlayerNumber);
                UnitReference.ActionPoints -= 1;
            }

            yield return null;
        }

        public override void Display(CellGrid cellGrid)
        {
            if (CanPerform(cellGrid))
            {
                ActivationButton.gameObject.SetActive(true);
            }
        }

        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            if (CapturingStructure != null)
            {
                if (!UnitReference.Cell.CurrentUnits.Contains(CapturingStructure))
                {
                    CapturingStructure.GetComponent<CapturableAbility>().Loyality = CapturingStructure.GetComponent<CapturableAbility>().MaxLoyality;
                    CapturingStructure.GetComponent<CapturableAbility>().UpdateLoyalityUI();
                    CapturingStructure = null;
                }
            }
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            if (ActivationButton != null)
            {
                ActivationButton.gameObject.SetActive(false);
            }
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            var capturable = GetComponent<Unit>().Cell.CurrentUnits.Select(u => u.GetComponent<CapturableAbility>())
                                                                  .OfType<CapturableAbility>()
                                                                  .ToList();

            return capturable.Count > 0 && capturable[0].GetComponent<Unit>().PlayerNumber != GetComponent<Unit>().PlayerNumber && UnitReference.ActionPoints > 0;
        }

        public void Activate()
        {
            var cellGrid = FindObjectOfType<CellGrid>();
            if (CanPerform(cellGrid))
            {
                StartCoroutine(HumanExecute(cellGrid));
                ActivationButton.gameObject.SetActive(false);
            }
        }

        public override void OnUnitDestroyed(CellGrid cellGrid)
        {
            if (CapturingStructure != null)
            {
                CapturingStructure.GetComponent<CapturableAbility>().Loyality = CapturingStructure.GetComponent<CapturableAbility>().MaxLoyality;
                CapturingStructure.GetComponent<CapturableAbility>().UpdateLoyalityUI();
                CapturingStructure = null;
            }
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
