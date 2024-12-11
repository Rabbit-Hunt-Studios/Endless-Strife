using UnityEngine;

namespace TbsFramework.Units.Highlighters
{
    public abstract class UnitHighlighter : MonoBehaviour
    {
        public abstract void Apply(Unit unit, Unit otherUnit);
    }
}