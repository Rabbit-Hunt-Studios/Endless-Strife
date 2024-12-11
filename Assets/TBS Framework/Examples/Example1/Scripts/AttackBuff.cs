using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Example1
{
    [CreateAssetMenu]
    public class AttackBuff : Buff
    {
        public int Factor;

        public override void Apply(Unit unit)
        {
            unit.AttackFactor += Factor;
        }

        public override void Undo(Unit unit)
        {
            unit.AttackFactor -= Factor;
        }
    }
}
