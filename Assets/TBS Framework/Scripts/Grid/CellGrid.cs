using System;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid.GameResolvers;
using TbsFramework.Grid.GridStates;
using TbsFramework.Grid.TurnResolvers;
using TbsFramework.Grid.UnitGenerators;
using TbsFramework.Players;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Grid
{
    /// <summary>
    /// CellGrid class keeps track of the game, stores cells, units and players objects. It starts the game and makes turn transitions. 
    /// It reacts to user interacting with units or cells, and raises events related to game progress. 
    /// </summary>
    public partial class CellGrid : MonoBehaviour
    {
        /// <summary>
        /// LevelLoading event is invoked before Initialize method is run.
        /// </summary>
        public event EventHandler LevelLoading;
        /// <summary>
        /// LevelLoadingDone event is invoked after Initialize method has finished running.
        /// </summary>
        public event EventHandler LevelLoadingDone;
        /// <summary>
        /// GameStarted event is invoked at the beggining of StartGame method.
        /// </summary>
        public event EventHandler GameStarted;
        /// <summary>
        /// GameEnded event is invoked when there is a single player left in the game.
        /// </summary>
        public event EventHandler<GameEndedArgs> GameEnded;
        /// <summary>
        /// Turn ended event is invoked at the end of each turn.
        /// </summary>
        public event EventHandler<bool> TurnEnded;

        /// <summary>
        /// UnitAdded event is invoked each time AddUnit method is called.
        /// </summary>
        public event EventHandler<UnitCreatedEventArgs> UnitAdded;

        private CellGridState _cellGridState;
        public CellGridState cellGridState
        {
            get
            {
                return _cellGridState;
            }
            set
            {
                CellGridState nextState;
                if (_cellGridState != null)
                {
                    _cellGridState.OnStateExit();
                    nextState = _cellGridState.MakeTransition(value);
                }
                else
                {
                    nextState = value;
                }

                _cellGridState = nextState;
                _cellGridState.OnStateEnter();
            }
        }

        public int NumberOfPlayers { get { return Players.Count; } }

        public Player CurrentPlayer
        {
            get { return Players.Find(p => p.PlayerNumber.Equals(CurrentPlayerNumber)); }
        }
        public int CurrentPlayerNumber { get; private set; }

        [HideInInspector]
        public bool Is2D;

        /// <summary>
        /// GameObject that holds player objects.
        /// </summary>
        public Transform PlayersParent;
        public bool ShouldStartGameImmediately = true;

        private int UnitId = 0;

        public bool GameFinished { get; private set; }
        public List<Player> Players { get; private set; }
        public List<Cell> Cells { get; private set; }
        public List<Unit> Units { get; private set; }
        private Func<List<Unit>> PlayableUnits = () => new List<Unit>();

        private void Start()
        {
            if (ShouldStartGameImmediately)
            {
                InitializeAndStart();
            }
        }

        public void InitializeAndStart()
        {
            Initialize();
            StartGame();
        }

        public void Initialize()
        {
            if (LevelLoading != null)
                LevelLoading.Invoke(this, EventArgs.Empty);

            GameFinished = false;
            Players = new List<Player>();
            for (int i = 0; i < PlayersParent.childCount; i++)
            {
                var player = PlayersParent.GetChild(i).GetComponent<Player>();
                if (player != null && player.gameObject.activeInHierarchy)
                {
                    player.Initialize(this);
                    Players.Add(player);
                }
            }

            Cells = new List<Cell>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var cell = transform.GetChild(i).gameObject.GetComponent<Cell>();
                if (cell != null)
                {
                    if (cell.gameObject.activeInHierarchy)
                    {
                        Cells.Add(cell);
                        cell.Initialize(this);
                    }
                }
                else
                {
                    Debug.LogError("Invalid object in cells parent game object");
                }
            }

            foreach (var cell in Cells)
            {
                cell.CellClicked += OnCellClicked;
                cell.CellHighlighted += OnCellHighlighted;
                cell.CellDehighlighted += OnCellDehighlighted;
                cell.GetComponent<Cell>().GetNeighbours(Cells);
            }

            Units = new List<Unit>();
            var unitGenerator = GetComponent<IUnitGenerator>();
            if (unitGenerator != null)
            {
                var units = unitGenerator.SpawnUnits(Cells);
                foreach (var unit in units)
                {
                    AddUnit(unit.GetComponent<Transform>());
                }
            }
            else
            {
                Debug.LogError("No IUnitGenerator script attached to cell grid");
            }

            if (LevelLoadingDone != null)
                LevelLoadingDone.Invoke(this, EventArgs.Empty);
        }

        private void OnCellDehighlighted(object sender, EventArgs e)
        {
            cellGridState.OnCellDeselected(sender as Cell);
        }
        private void OnCellHighlighted(object sender, EventArgs e)
        {
            cellGridState.OnCellSelected(sender as Cell);
        }
        private void OnCellClicked(object sender, EventArgs e)
        {
            cellGridState.OnCellClicked(sender as Cell);
        }

        private void OnUnitClicked(object sender, EventArgs e)
        {
            cellGridState.OnUnitClicked(sender as Unit);
        }
        private void OnUnitHighlighted(object sender, EventArgs e)
        {
            cellGridState.OnUnitHighlighted(sender as Unit);
        }
        private void OnUnitDehighlighted(object sender, EventArgs e)
        {
            cellGridState.OnUnitDehighlighted(sender as Unit);
        }

        private void OnUnitDestroyed(object sender, AttackEventArgs e)
        {
            Units.Remove(e.Defender);
            e.Defender.GetComponents<Ability>().ToList().ForEach(a => a.OnUnitDestroyed(this));
            e.Defender.UnitClicked -= OnUnitClicked;
            e.Defender.UnitHighlighted -= OnUnitHighlighted;
            e.Defender.UnitDehighlighted -= OnUnitDehighlighted;
            e.Defender.UnitDestroyed -= OnUnitDestroyed;
            e.Defender.UnitMoved -= OnUnitMoved;
            CheckGameFinished();
        }

        /// <summary>
        /// Adds unit to the game
        /// </summary>
        /// <param name="unit">Unit to add</param>
        public void AddUnit(Transform unit, Cell targetCell = null, Player ownerPlayer = null)
        {
            unit.GetComponent<Unit>().UnitID = UnitId++;
            Units.Add(unit.GetComponent<Unit>());

            if (targetCell != null)
            {
                targetCell.IsTaken = unit.GetComponent<Unit>().Obstructable;

                unit.GetComponent<Unit>().Cell = targetCell;
                unit.GetComponent<Unit>().transform.localPosition = targetCell.transform.localPosition;
            }

            if (ownerPlayer != null)
            {
                unit.GetComponent<Unit>().PlayerNumber = ownerPlayer.PlayerNumber;
            }

            if(unit.GetComponent<Unit>().Cell != null)
            {
                unit.GetComponent<Unit>().Cell.CurrentUnits.Add(unit.GetComponent<Unit>());
            }

            unit.GetComponent<Unit>().transform.localRotation = Quaternion.Euler(0, 0, 0);
            unit.GetComponent<Unit>().Initialize();

            unit.GetComponent<Unit>().UnitClicked += OnUnitClicked;
            unit.GetComponent<Unit>().UnitHighlighted += OnUnitHighlighted;
            unit.GetComponent<Unit>().UnitDehighlighted += OnUnitDehighlighted;
            unit.GetComponent<Unit>().UnitDestroyed += OnUnitDestroyed;
            unit.GetComponent<Unit>().UnitMoved += OnUnitMoved;

            if (UnitAdded != null)
            {
                UnitAdded.Invoke(this, new UnitCreatedEventArgs(unit));
            }
        }

        private void OnUnitMoved(object sender, MovementEventArgs e)
        {
            CheckGameFinished();
        }

        /// <summary>
        /// Method is called once, at the beggining of the game.
        /// </summary>
        public void StartGame()
        {
            TransitionResult transitionResult = GetComponent<TurnResolver>().ResolveStart(this);
            PlayableUnits = transitionResult.PlayableUnits;
            CurrentPlayerNumber = transitionResult.NextPlayer.PlayerNumber;

            GameStarted?.Invoke(this, EventArgs.Empty);

            PlayableUnits().ForEach(u => { u.GetComponents<Ability>().ToList().ForEach(a => a.OnTurnStart(this)); u.OnTurnStart(); });
            CurrentPlayer.Play(this);
            Debug.Log("Game started");
        }

        public void EndTurn(bool isNetworkInvoked=false)
        {
            _cellGridState.EndTurn(isNetworkInvoked);
        }

        /// <summary>
        /// Method makes the actual turn transitions.
        /// </summary>
        private void EndTurnExecute(bool isNetworkInvoked=false)
        {
            cellGridState = new CellGridStateBlockInput(this);
            bool isGameFinished = CheckGameFinished();
            if (isGameFinished)
            {
                return;
            }

            var playableUnits = PlayableUnits();
            for (int i = 0; i < playableUnits.Count; i++)
            {
                var unit = playableUnits[i];
                if (unit == null)
                {
                    continue;
                }

                unit.OnTurnEnd();
                var abilities = unit.GetComponents<Ability>();
                for (int j = 0; j < abilities.Length; j++)
                {
                    var ability = abilities[j];
                    ability.OnTurnEnd(this);
                }
            }
            TransitionResult transitionResult = GetComponent<TurnResolver>().ResolveTurn(this);

            PlayableUnits = transitionResult.PlayableUnits;
            CurrentPlayerNumber = transitionResult.NextPlayer.PlayerNumber;

            if (TurnEnded != null)
                TurnEnded.Invoke(this, isNetworkInvoked);

            Debug.Log(string.Format("Player {0} turn", CurrentPlayerNumber));

            playableUnits = PlayableUnits();
            for (int i = 0; i < playableUnits.Count; i++)
            {
                var unit = playableUnits[i];
                if (unit == null)
                {
                    continue;
                }

                var abilities = unit.GetComponents<Ability>();
                for (int j = 0; j < abilities.Length; j++)
                {
                    var ability = abilities[j];
                    ability.OnTurnStart(this);
                }
                unit.OnTurnStart();
            }
            CurrentPlayer.Play(this);
        }

        public List<Unit> GetCurrentPlayerUnits()
        {
            return PlayableUnits();
        }
        public List<Unit> GetEnemyUnits(Player player)
        {
            return Units.FindAll(u => u.PlayerNumber != player.PlayerNumber);
        }
        public List<Unit> GetPlayerUnits(Player player)
        {
            return Units.FindAll(u => u.PlayerNumber == player.PlayerNumber);
        }

        public bool CheckGameFinished()
        {
            List<GameResult> gameResults =
                GetComponents<GameEndCondition>()
                .Select(c => c.CheckCondition(this))
                .ToList();

            foreach (var gameResult in gameResults)
            {
                if (gameResult.IsFinished)
                {
                    cellGridState = new CellGridStateGameOver(this);
                    GameFinished = true;
                    if (GameEnded != null)
                    {
                        GameEnded.Invoke(this, new GameEndedArgs(gameResult));
                    }

                    break;
                }
            }
            return GameFinished;
        }
    }
}

