using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Example1
{
    [CreateAssetMenu]
    public class HealingBuff : Buff
    {
        public int HealingFactor;

        public HealingBuff(int duration, int healingFactor)
        {
            HealingFactor = healingFactor;
            Duration = duration;
        }

        public override void Apply(Unit unit)
        {
            AddHitPoints(unit, HealingFactor);
        }
        public override void Undo(Unit unit)
        {
            //Note that healing buff has empty Undo method implementation.
        }

        private void AddHitPoints(Unit unit, int amount)
        {
            unit.HitPoints = Mathf.Clamp(unit.HitPoints + amount, 0, unit.TotalHitPoints);
        }
    }
}
