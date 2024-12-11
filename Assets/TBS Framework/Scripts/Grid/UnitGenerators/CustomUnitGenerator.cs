using System;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Grid.UnitGenerators
{
    public class CustomUnitGenerator : MonoBehaviour, IUnitGenerator
    {
        public Transform UnitsParent;
        public Transform CellsParent;

        /// <summary>
        /// Returns units that are children of UnitsParent object.
        /// </summary>
        public List<Unit> SpawnUnits(List<Cell> cells)
        {
            List<Unit> ret = new List<Unit>();
            for (int i = 0; i < UnitsParent.childCount; i++)
            {
                var unit = UnitsParent.GetChild(i).GetComponent<Unit>();
                if (unit != null)
                {
                    ret.Add(unit);
                }
                else
                {
                    Debug.LogError("Invalid object in Units Parent game object");
                }
            }
            return ret;
        }

        public void SnapToGrid()
        {

        }
    }
}
