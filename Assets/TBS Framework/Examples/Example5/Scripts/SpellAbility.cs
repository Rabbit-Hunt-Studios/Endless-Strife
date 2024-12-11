using System.Collections;
using TbsFramework.Grid;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public abstract class SpellAbility : Ability
    {
        public Sprite Image;
        public string SpellName;
        public int ManaCost;

        public GameObject CancelButton;
        public string Description;

        public abstract string GetDetails();

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            UnitReference.GetComponent<SpellCastingAbility>().CurrentMana -= ManaCost;
            yield return null;
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            CancelButton.SetActive(false);
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return ManaCost <= UnitReference.GetComponent<SpellCastingAbility>().CurrentMana;
        }
    }
}