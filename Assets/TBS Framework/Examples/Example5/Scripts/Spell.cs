using TbsFramework.Grid;
using TbsFramework.Units.Abilities;

namespace TbsFramework.HOMMExample
{
    public abstract class Spell : Ability
    {
        public int ManaCost;

        public override bool CanPerform(CellGrid cellGrid)
        {
            return ManaCost <= UnitReference.GetComponent<SpellCastingAbility>().CurrentMana;
        }
    }
}