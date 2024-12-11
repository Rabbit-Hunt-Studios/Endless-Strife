using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Example1
{
    [CreateAssetMenu]
    public class DefenceBuff : Buff
    {
        public int Factor;

        public override void Apply(Unit unit)
        {
            unit.DefenceFactor += Factor;
        }

        public override void Undo(Unit unit)
        {
            unit.DefenceFactor -= Factor;
        }
    }
}

