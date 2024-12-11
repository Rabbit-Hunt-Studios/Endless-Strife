using UnityEngine;

namespace TbsFramework.Units
{
    /// <summary>
    /// Class representing an "upgrade" to a unit.
    /// </summary>
    public abstract class Buff : ScriptableObject
    {
        /// <summary>
        /// Determines how long the buff should last (expressed in turns). If set to negative number, buff will be permanent.
        /// </summary>
        public int Duration;

        /// <summary>
        /// Describes how the unit should be upgraded.
        /// </summary>
        public abstract void Apply(Unit unit);
        /// <summary>
        /// Returns units stats to normal.
        /// </summary>
        public abstract void Undo(Unit unit);
    }
}