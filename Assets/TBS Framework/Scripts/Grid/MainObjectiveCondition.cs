using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Grid.GameResolvers;
using TbsFramework.Units;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.Grid
{
    public class MainObjectiveCondition : GameEndCondition
    {
        public CellGrid CellGrid;
        public Unit StructureToCapture;
        public int CaptureTurns;
        public Text TurnCounter;
        private Dictionary<int, int> TargetPlayerTurnCount;
        private int lastCheckedTurn = -1;

        void Awake()
        {
            InitializeTurnCounter();
            CellGrid.TurnEnded += OnTurnEnded;
        }

        private void InitializeTurnCounter()
        {
            TurnCounter.text = $"0/{CaptureTurns}";
            TargetPlayerTurnCount = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } };
        }

        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            var currentPlayerNumber = cellGrid.CurrentPlayerNumber;

            Debug.Log($"Checking condition for player {currentPlayerNumber}");

            if (TargetPlayerTurnCount[currentPlayerNumber] >= CaptureTurns)
            {
                return new GameResult(true, new List<int> { currentPlayerNumber }, new List<int>());
            }

            if (currentPlayerNumber == lastCheckedTurn)
            {
                return new GameResult(false, new List<int>(), new List<int>());
            }

            lastCheckedTurn = currentPlayerNumber;
            TurnCounter.text = $"{TargetPlayerTurnCount[currentPlayerNumber]}/{CaptureTurns}";

            if (StructureToCapture.PlayerNumber == currentPlayerNumber)
            {
                TargetPlayerTurnCount[currentPlayerNumber]++;
                TurnCounter.text = $"{TargetPlayerTurnCount[currentPlayerNumber]}/{CaptureTurns}";
            }
            return new GameResult(false, new List<int>(), new List<int>());
        }

        void OnTurnEnded(object sender, bool isNetworkInvoked)
        {
            var cellGrid = sender as CellGrid;
            var currentPlayerNumber = cellGrid.CurrentPlayerNumber;

            TurnCounter.text = $"{TargetPlayerTurnCount[currentPlayerNumber]}/{CaptureTurns}";

            if (StructureToCapture.PlayerNumber == currentPlayerNumber)
            {
                TargetPlayerTurnCount[currentPlayerNumber]++;
                TurnCounter.text = $"{TargetPlayerTurnCount[currentPlayerNumber]}/{CaptureTurns}";;
                lastCheckedTurn = currentPlayerNumber;
            }
        }
    }
}