using System;
using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI
{
    public abstract class UnitSelection : MonoBehaviour
    {
        public abstract IEnumerable<Unit> SelectNext(Func<List<Unit>> getUnits, CellGrid cellGrid);
    }
}