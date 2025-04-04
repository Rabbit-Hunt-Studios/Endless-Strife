﻿using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class DamageRecievedCellEvaluator : CellEvaluator
    {
        Dictionary<Cell, float> totalDamagePerCell;

        public override void Precalculate(Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            AudioController audioController = GameObject.Find("AudioController").GetComponent<AudioController>();
            audioController.SFXSource.mute = true; 
            totalDamagePerCell = new Dictionary<Cell, float>();
            foreach (var cell in cellGrid.Cells)
            {
                totalDamagePerCell[cell] = 0f;
            }

            var enemyUnits = cellGrid.GetEnemyUnits(currentPlayer);
            foreach (var unit in enemyUnits)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                    
                var cellsInAttackRange = cellGrid.Cells.Where(c => evaluatingUnit.IsCellMovableTo(c) && unit.Cell.GetDistance(c) <= unit.MovementPoints + unit.AttackRange);
                var damage = unit.DryAttack(evaluatingUnit);
                foreach (var cellInAttackRange in cellsInAttackRange)
                {
                    totalDamagePerCell[cellInAttackRange] += damage;
                }
            }
            audioController.SFXSource.mute = false;
        }

        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var totalDamage = totalDamagePerCell[cellToEvaluate];
            return evaluatingUnit.HitPoints - totalDamage <= 0 ? -1 : (float)totalDamage / evaluatingUnit.HitPoints * (-1);
        }
    }
}
