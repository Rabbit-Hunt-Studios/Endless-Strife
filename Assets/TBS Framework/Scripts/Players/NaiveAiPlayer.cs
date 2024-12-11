using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Players
{
    /// <summary>
    /// Simple implementation of AI for the game.
    /// </summary>
    [Obsolete("Replaced with AIPlayer", false)]
    public class NaiveAiPlayer : Player
    {
        private CellGrid _cellGrid;
        private System.Random _rnd;

        public NaiveAiPlayer()
        {
            _rnd = new System.Random();
        }

        public override void Play(CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid);
            _cellGrid = cellGrid;

            StartCoroutine(Play());

            //Coroutine is necessary to allow Unity to run updates on other objects (like UI).
            //Implementing this with threads would require a lot of modifications in other classes, as Unity API is not thread safe.
        }
        private IEnumerator Play()
        {
            var myUnits = _cellGrid.GetCurrentPlayerUnits();

            foreach (var unit in myUnits.OrderByDescending(u => u.Cell.GetNeighbours(_cellGrid.Cells).FindAll(u.IsCellTraversable).Count))
            {
                var enemyUnits = _cellGrid.GetEnemyUnits(this);
                var unitsInRange = new List<Unit>();
                foreach (var enemyUnit in enemyUnits)
                {
                    if (unit.IsUnitAttackable(enemyUnit, unit.Cell))
                    {
                        unitsInRange.Add(enemyUnit);
                    }
                }//Looking for enemies that are in attack range.
                if (unitsInRange.Count != 0)
                {
                    var index = _rnd.Next(0, unitsInRange.Count);
                    unit.GetComponent<AttackAbility>().UnitToAttack = unitsInRange[index];
                    StartCoroutine(unit.GetComponent<AttackAbility>().Act(_cellGrid, false));
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }//If there is an enemy in range, attack it.

                List<Cell> potentialDestinations = new List<Cell>();

                foreach (var enemyUnit in enemyUnits)
                {
                    potentialDestinations.AddRange(_cellGrid.Cells.FindAll(c => unit.IsCellMovableTo(c) && unit.IsUnitAttackable(enemyUnit, c)));
                }//Making a list of cells that the unit can attack from.


                var notInRange = potentialDestinations.FindAll(c => c.GetDistance(unit.Cell) > unit.MovementPoints);
                potentialDestinations = potentialDestinations.Except(notInRange).ToList();

                if (potentialDestinations.Count == 0 && notInRange.Count != 0)
                {
                    potentialDestinations.Add(notInRange.ElementAt(_rnd.Next(0, notInRange.Count - 1)));
                }

                potentialDestinations = potentialDestinations.OrderBy(h => _rnd.Next()).ToList();
                IList<Cell> shortestPath = null;
                foreach (var potentialDestination in potentialDestinations)
                {
                    var path = unit.FindPath(_cellGrid.Cells, potentialDestination);
                    if ((shortestPath == null && path.Sum(h => h.MovementCost) > 0) || shortestPath != null && (path.Sum(h => h.MovementCost) < shortestPath.Sum(h => h.MovementCost) && path.Sum(h => h.MovementCost) > 0))
                        shortestPath = path;

                    var pathCost = path.Sum(h => h.MovementCost);
                    if (pathCost > 0 && pathCost <= unit.MovementPoints)
                    {
                        unit.GetComponent<MoveAbility>().Destination = potentialDestination;
                        yield return StartCoroutine(unit.GetComponent<MoveAbility>().Act(_cellGrid, false));
                        shortestPath = null;
                        break;
                    }
                    yield return null;
                }//If there is a path to any cell that the unit can attack from, move there.

                if (shortestPath != null)
                {
                    foreach (var potentialDestination in shortestPath.Intersect(unit.GetAvailableDestinations(_cellGrid.Cells)).OrderByDescending(h => h.GetDistance(unit.Cell)))
                    {
                        var path = unit.FindPath(_cellGrid.Cells, potentialDestination);
                        var pathCost = path.Sum(h => h.MovementCost);
                        if (pathCost > 0 && pathCost <= unit.MovementPoints)
                        {
                            unit.GetComponent<MoveAbility>().Destination = potentialDestination;
                            yield return StartCoroutine(unit.GetComponent<MoveAbility>().Act(_cellGrid, false));
                            break;
                        }
                        yield return null;
                    }
                }//If the path cost is greater than unit movement points, move as far as possible.

                foreach (var enemyUnit in enemyUnits)
                {
                    var enemyCell = enemyUnit.Cell;
                    if (unit.IsUnitAttackable(enemyUnit, unit.Cell))
                    {
                        unit.GetComponent<AttackAbility>().UnitToAttack = enemyUnit;
                        StartCoroutine(unit.GetComponent<AttackAbility>().Act(_cellGrid, false));
                        yield return new WaitForSeconds(0.5f);
                        break;
                    }
                }//Look for enemies in range and attack.
            }
            _cellGrid.EndTurn();
        }
    }
}