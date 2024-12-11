using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using TbsFramework.Units.UnitStates;
using UnityEngine;

namespace TbsFramework.Grid.GridStates
{
    public class CellGridStateAbilitySelected : CellGrid.CellGridState
    {
        List<Ability> _abilities;
        Unit _unit;

        public CellGridStateAbilitySelected(CellGrid cellGrid, Unit unit, List<Ability> abilities) : base(cellGrid)
        {
            if (abilities.Count == 0)
            {
                Debug.LogError("No abilities were selected, check if your unit has any abilities attached to it");
            }

            _abilities = abilities;
            _unit = unit;
        }

        public CellGridStateAbilitySelected(CellGrid cellGrid, Unit unit, Ability ability) : this(cellGrid, unit, new List<Ability>() { ability }) { }

        public override void OnUnitClicked(Unit unit)
        {
            _abilities.ForEach(a => a.OnUnitClicked(unit, _cellGrid));
        }
        public override void OnUnitHighlighted(Unit unit)
        {
            _abilities.ForEach(a => a.OnUnitHighlighted(unit, _cellGrid));
        }
        public override void OnUnitDehighlighted(Unit unit)
        {
            _abilities.ForEach(a => a.OnUnitDehighlighted(unit, _cellGrid));
        }
        public override void OnCellClicked(Cell cell)
        {
            _abilities.ForEach(a => a.OnCellClicked(cell, _cellGrid));
        }
        public override void OnCellSelected(Cell cell)
        {
            base.OnCellSelected(cell);
            _abilities.ForEach(a => a.OnCellSelected(cell, _cellGrid));
        }
        public override void OnCellDeselected(Cell cell)
        {
            base.OnCellDeselected(cell);
            _abilities.ForEach(a => a.OnCellDeselected(cell, _cellGrid));
        }

        public override void OnStateEnter()
        {
            _unit?.OnUnitSelected();
            _abilities.ForEach(a => a.OnAbilitySelected(_cellGrid));
            _abilities.ForEach(a => a.Display(_cellGrid));

            var canPerformAction = _abilities.Select(a => a.CanPerform(_cellGrid))
                                             .DefaultIfEmpty()
                                             .Aggregate((result, next) => result || next);
            if (!canPerformAction)
            {
                _unit?.SetState(new UnitStateMarkedAsFinished(_unit));
            }
        }
        public override void OnStateExit()
        {
            _unit?.OnUnitDeselected();
            _abilities.ForEach(a => a.OnAbilityDeselected(_cellGrid));
            _abilities.ForEach(a => a.CleanUp(_cellGrid));
        }
    }
}
