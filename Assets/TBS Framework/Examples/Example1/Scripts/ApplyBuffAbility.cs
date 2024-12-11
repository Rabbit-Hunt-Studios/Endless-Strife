using System.Collections;
using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine.UI;

namespace TbsFramework.Example1
{
    public class ApplyBuffAbility : Ability
    {
        public Buff Buff;
        public int Amount = 1;
        public Button ActivationButton;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (Amount != 0)
            {
                UnitReference.AddBuff(Buff);
                UnitReference.MarkAsDefending(null);

                Amount--;
            }

            yield return null;
        }

        public override void Display(CellGrid cellGrid)
        {
            if (Amount != 0 && ActivationButton != null)
            {
                ActivationButton.gameObject.SetActive(true);
            }
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            if (ActivationButton != null)
            {
                ActivationButton.gameObject.SetActive(false);
            }
        }

        public void Activate()
        {
            StartCoroutine(Act(FindObjectOfType<CellGrid>(), false));
            if (Amount == 0)
            {
                ActivationButton.gameObject.SetActive(false);
            }
        }
    }
}