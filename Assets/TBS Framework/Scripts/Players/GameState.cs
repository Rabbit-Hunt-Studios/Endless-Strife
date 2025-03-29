using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI
{
    public enum StructureType
    {
        None,
        Base,
        Outpost,
        Objective
    }
    
    public class Structure
    {
        public StructureType Type;
        public int OwnerPlayerNumber;
        public int OriginalOwnerPlayerNumber;
        public Vector2Int Position;
        
        public Structure Clone()
        {
            return new Structure
            {
                Type = this.Type,
                OwnerPlayerNumber = this.OwnerPlayerNumber,
                OriginalOwnerPlayerNumber = this.OriginalOwnerPlayerNumber,
                Position = this.Position
            };
        }
    }
    
    public class GameState
    {
        public CellGrid CellGrid;
        public int CurrentPlayerNumber;
        public List<Unit> Units;
        public List<Structure> Structures;
        public Dictionary<int, int> Resources;
        public Dictionary<int, int> ObjectiveControlTurns;
        public int Winner = -1;
        
        public GameState(CellGrid cellGrid, int currentPlayerNumber)
        {
            CellGrid = cellGrid;
            CurrentPlayerNumber = currentPlayerNumber;
            Units = new List<Unit>(cellGrid.Units);
            Structures = ExtractStructures(cellGrid);
            Resources = ExtractResources(cellGrid);
            ObjectiveControlTurns = ExtractObjectiveControlTurns(cellGrid);
            Winner = DetermineWinner(cellGrid);
        }
        
        private GameState()
        {
            // For cloning
        }
        
        public GameState Clone()
        {
            GameState clone = new GameState();
            clone.CellGrid = CellGrid; // Reference only, not deep copying
            clone.CurrentPlayerNumber = CurrentPlayerNumber;
            clone.Units = new List<Unit>(Units); // Shallow copy of units
            clone.Structures = Structures.Select(s => s.Clone()).ToList();
            clone.Resources = new Dictionary<int, int>(Resources);
            clone.ObjectiveControlTurns = new Dictionary<int, int>(ObjectiveControlTurns);
            clone.Winner = Winner;
            return clone;
        }
        
        public bool IsGameOver()
        {
            return Winner >= 0;
        }
        
        private List<Structure> ExtractStructures(CellGrid cellGrid)
        {
            List<Structure> structures = new List<Structure>();
            
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    StructureType type = StructureType.None;
                    int ownerPlayerNumber = 0;
                    int originalOwnerPlayerNumber = 0;
                    
                    foreach (var unit in cell.CurrentUnits)
                    {
                        if (unit == null || !unit.isActiveAndEnabled)
                            continue;
                        if (unit is ESUnit esUnit && esUnit.isStructure)
                        {
                            if (esUnit.UnitName.Contains("Base"))
                                type = StructureType.Base;
                            else if (esUnit.UnitName.Contains("Outpost"))
                                type = StructureType.Outpost;
                            else if (esUnit.UnitName.Contains("Objective"))
                                type = StructureType.Objective;
                            
                            ownerPlayerNumber = unit.PlayerNumber;
                            break;
                        }
                    }
                    
                    if (type != StructureType.None)
                    {
                        structures.Add(new Structure
                        {
                            Type = type,
                            OwnerPlayerNumber = ownerPlayerNumber,
                            OriginalOwnerPlayerNumber = originalOwnerPlayerNumber,
                            Position = new Vector2Int((int)cell.OffsetCoord.x, (int)cell.OffsetCoord.y)
                        });
                    }
                }
            }
            
            return structures;
        }
        
        private Dictionary<int, int> ExtractResources(CellGrid cellGrid)
        {
            Dictionary<int, int> resources = new Dictionary<int, int>();
            EconomyController economy = Object.FindObjectOfType<EconomyController>();
            
            if (economy != null)
            {
                foreach (var player in cellGrid.Players)
                {
                    resources[player.PlayerNumber] = economy.GetValue(player.PlayerNumber);
                }
            }
            
            return resources;
        }
        
        private Dictionary<int, int> ExtractObjectiveControlTurns(CellGrid cellGrid)
        {
            Dictionary<int, int> controlTurns = new Dictionary<int, int>();
            
            foreach (var player in cellGrid.Players)
            {
                // This is a simplification - you would need to retrieve the actual control turns from your game
                controlTurns[player.PlayerNumber] = 0;
                
                if (player is MinimaxAIPlayer minimaxPlayer)
                {
                    // Get the objective control turns tracker from the player
                    var objectiveTurnsField = typeof(MinimaxAIPlayer)
                        .GetField("objectiveControlTurns", 
                                 System.Reflection.BindingFlags.NonPublic | 
                                 System.Reflection.BindingFlags.Instance);
                    
                    if (objectiveTurnsField != null)
                    {
                        controlTurns[player.PlayerNumber] = (int)objectiveTurnsField.GetValue(minimaxPlayer);
                    }
                }
            }
            
            return controlTurns;
        }
        
        private int DetermineWinner(CellGrid cellGrid)
        {
            // Check winning conditions based on your game rules
            
            // 1. Objective control for 5 turns
            foreach (var kvp in ObjectiveControlTurns)
            {
                if (kvp.Value >= 5)
                    return kvp.Key;
            }
            
            // 2. Base capture
            foreach (var structure in Structures)
            {
                if (structure.Type == StructureType.Base && 
                    structure.OwnerPlayerNumber != structure.OriginalOwnerPlayerNumber)
                {
                    return structure.OwnerPlayerNumber;
                }
            }
            
            return -1; // No winner yet
        }
    }
}