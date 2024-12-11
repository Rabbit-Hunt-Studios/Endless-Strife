using TbsFramework.Units;
using TbsFramework.Units.Highlighters;
using UnityEngine;

namespace TbsFramework.Example4
{
    public class DisableComponent : UnitHighlighter
    {
        public MonoBehaviour ToDisable;

        public override void Apply(Unit unit, Unit otherUnit)
        {
            ToDisable.enabled = false;
        }
    }
}

