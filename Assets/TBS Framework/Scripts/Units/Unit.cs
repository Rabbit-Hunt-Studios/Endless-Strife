using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TbsFramework.Cells;
using TbsFramework.Pathfinding.Algorithms;
using TbsFramework.Units.Highlighters;
using TbsFramework.Units.UnitStates;
using TbsFramework.Grid;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units.Abilities;

namespace TbsFramework.Units
{
    /// <summary>
    /// Base class for all units in the game.
    /// </summary>
    [ExecuteInEditMode]
    public class Unit : MonoBehaviour
    {
        Dictionary<Cell, IList<Cell>> cachedPaths = null;
        /// <summary>
        /// UnitClicked event is invoked when user clicks the unit. 
        /// It requires a collider on the unit game object to work.
        /// </summary>
        public event EventHandler UnitClicked;
        /// <summary>
        /// UnitSelected event is invoked when user clicks on unit that belongs to him. 
        /// It requires a collider on the unit game object to work.
        /// </summary>
        public event EventHandler UnitSelected;
        /// <summary>
        /// UnitDeselected event is invoked when user click outside of currently selected unit's collider.
        /// It requires a collider on the unit game object to work.
        /// </summary>
        public event EventHandler UnitDeselected;
        /// <summary>
        /// UnitHighlighted event is invoked when user moves cursor over the unit. 
        /// It requires a collider on the unit game object to work.
        /// </summary>
        public event EventHandler UnitHighlighted;
        /// <summary>
        /// UnitDehighlighted event is invoked when cursor exits unit's collider. 
        /// It requires a collider on the unit game object to work.
        /// </summary>
        public event EventHandler UnitDehighlighted;
        /// <summary>
        /// UnitAttacked event is invoked when the unit is attacked.
        /// </summary>
        public event EventHandler<AttackEventArgs> UnitAttacked;
        /// <summary>
        /// UnitDestroyed event is invoked when unit's hitpoints drop below 0.
        /// </summary>
        public event EventHandler<AttackEventArgs> UnitDestroyed;
        /// <summary>
        /// UnitMoved event is invoked when unit moves from one cell to another.
        /// </summary>
        public event EventHandler<MovementEventArgs> UnitMoved;
        public event EventHandler<AbilityAddedEventArgs> AbilityAddded;

        public UnitHighlighterAggregator UnitHighlighterAggregator;

        private int AbilityID = 0;

        public List<Ability> Abilities { get; private set; } = new List<Ability>();
        public void RegisterAbility(Ability ability)
        {
            ability.AbilityID = AbilityID++;
            ability.UnitReference = this;
            Abilities.Add(ability);

            if (AbilityAddded != null)
            {
                AbilityAddded.Invoke(this, new AbilityAddedEventArgs(ability));
            }
        }
        public int UnitID { get; set; }

        public bool Obstructable = true;

        public UnitState UnitState { get; set; }
        public void SetState(UnitState state)
        {
            UnitState.MakeTransition(state);
        }
        /// <summary>
        /// A list of buffs that are applied to the unit.
        /// </summary>
        private List<(Buff buff, int timeLeft)> Buffs;
        public void AddBuff(Buff buff)
        {
            buff.Apply(this);
            Buffs.Add((buff, buff.Duration));
        }

        public int TotalHitPoints { get; private set; }
        public float TotalMovementPoints { get; private set; }
        public float TotalActionPoints { get; private set; }

        /// <summary>
        /// Cell that the unit is currently occupying.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private Cell cell;
        public Cell Cell
        {
            get
            {
                return cell;
            }
            set
            {
                cell = value;
            }
        }

        public int HitPoints;
        public int AttackRange;
        public int AttackFactor;
        public int DefenceFactor;
        /// <summary>
        /// Determines how far on the grid the unit can move.
        /// </summary>
        [SerializeField]
        private float movementPoints;
        public virtual float MovementPoints
        {
            get
            {
                return movementPoints;
            }
            set
            {
                movementPoints = value;
            }
        }
        /// <summary>
        /// Determines speed of movement animation.
        /// </summary>
        public float MovementAnimationSpeed;
        /// <summary>
        /// Determines how many attacks unit can perform in one turn.
        /// </summary>
        [SerializeField]
        private float actionPoints = 1;
        public float ActionPoints
        {
            get
            {
                return actionPoints;
            }
            set
            {
                actionPoints = value;
            }
        }

        /// <summary>
        /// Indicates the player that the unit belongs to. 
        /// Should correspoond with PlayerNumber variable on Player script.
        /// </summary>
        public int PlayerNumber;

        private static DijkstraPathfinding _pathfinder = new DijkstraPathfinding();

        /// <summary>
        /// Method called when unit was added to the game to initialize fields etc. 
        /// </summary>
        public virtual void Initialize()
        {
            Buffs = new List<(Buff, int)>();

            UnitState = new UnitStateNormal(this);

            TotalHitPoints = HitPoints;
            TotalMovementPoints = MovementPoints;
            TotalActionPoints = ActionPoints;

            foreach(var ability in GetComponentsInChildren<Ability>())
            {
                RegisterAbility(ability);
                ability.Initialize();
            }
        }

        public virtual void OnMouseDown()
        {
            if (UnitClicked != null)
            {
                UnitClicked.Invoke(this, EventArgs.Empty);
            }
        }
        public virtual void OnMouseEnter()
        {
            if (UnitHighlighted != null)
            {
                UnitHighlighted.Invoke(this, EventArgs.Empty);
            }
        }
        public virtual void OnMouseExit()
        {
            if (UnitDehighlighted != null)
            {
                UnitDehighlighted.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Method is called at the start of each turn.
        /// </summary>
        public virtual void OnTurnStart()
        {
            cachedPaths = null;

            Buffs.FindAll(b => b.timeLeft == 0).ForEach(b => { b.buff.Undo(this); });
            Buffs.RemoveAll(b => b.timeLeft == 0);
            var name = this.name;
            var state = UnitState;
            SetState(new UnitStateMarkedAsFriendly(this));
        }
        /// <summary>
        /// Method is called at the end of each turn.
        /// </summary>
        public virtual void OnTurnEnd()
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                (Buff buff, int timeLeft) = Buffs[i];
                Buffs[i] = (buff, timeLeft - 1);
            }

            MovementPoints = TotalMovementPoints;
            ActionPoints = TotalActionPoints;

            SetState(new UnitStateNormal(this));
        }
        /// <summary>
        /// Method is called when units HP drops below 1.
        /// </summary>
        protected virtual void OnDestroyed()
        {
            Cell.IsTaken = false;
            Cell.CurrentUnits.Remove(this);
            MarkAsDestroyed();
            Destroy(gameObject);
        }

        /// <summary>
        /// Method is called when unit is selected.
        /// </summary>
        public virtual void OnUnitSelected()
        {
            if (FindObjectOfType<CellGrid>().GetCurrentPlayerUnits().Contains(this))
            {
                SetState(new UnitStateMarkedAsSelected(this));
            }
            if (UnitSelected != null)
            {
                UnitSelected.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Method is called when unit is deselected.
        /// </summary>
        public virtual void OnUnitDeselected()
        {
            if (FindObjectOfType<CellGrid>().GetCurrentPlayerUnits().Contains(this))
            {
                SetState(new UnitStateMarkedAsFriendly(this));
            }
            if (UnitDeselected != null)
            {
                UnitDeselected.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Method indicates if it is possible to attack a unit from given cell.
        /// </summary>
        /// <param name="other">Unit to attack</param>
        /// <param name="sourceCell">Cell to perform an attack from</param>
        /// <returns>Boolean value whether unit can be attacked or not</returns>
        public virtual bool IsUnitAttackable(Unit other, Cell sourceCell)
        {
            return IsUnitAttackable(other, other.Cell, sourceCell);
        }
        public virtual bool IsUnitAttackable(Unit other, Cell otherCell, Cell sourceCell)
        {
            return sourceCell.GetDistance(otherCell) <= AttackRange
                && other.PlayerNumber != PlayerNumber
                && ActionPoints >= 1;
        }


        /// <summary>
        /// Method performs an attack on given unit.
        /// </summary>
        public void AttackHandler(Unit unitToAttack)
        {
            AttackAction attackAction = DealDamage(unitToAttack);
            MarkAsAttacking(unitToAttack);
            unitToAttack.DefendHandler(this, attackAction.Damage);
            AttackActionPerformed(attackAction.ActionCost);
        }
        /// <summary>
        /// Method for calculating damage and action points cost of attacking given unit
        /// </summary>
        /// <returns></returns>
        protected virtual AttackAction DealDamage(Unit unitToAttack)
        {
            return new AttackAction(AttackFactor, 1f);
        }
        /// <summary>
        /// Method called after unit performed an attack.
        /// </summary>
        /// <param name="actionCost">Action point cost of performed attack</param>
        protected virtual void AttackActionPerformed(float actionCost)
        {
            ActionPoints -= actionCost;
        }

        /// <summary>
        /// Handler method for defending against an attack.
        /// </summary>
        /// <param name="aggressor">Unit that performed the attack</param>
        /// <param name="damage">Amount of damge that the attack caused</param>
        public void DefendHandler(Unit aggressor, int damage)
        {
            MarkAsDefending(aggressor);
            int damageTaken = Defend(aggressor, damage);
            HitPoints -= damageTaken;
            DefenceActionPerformed();

            if (UnitAttacked != null)
            {
                UnitAttacked.Invoke(this, new AttackEventArgs(aggressor, this, damage));
            }
            if (HitPoints <= 0)
            {
                if (UnitDestroyed != null)
                {
                    UnitDestroyed.Invoke(this, new AttackEventArgs(aggressor, this, damage));
                }
                OnDestroyed();
            }
        }
        /// <summary>
        /// Method for calculating actual damage taken by the unit.
        /// </summary>
        /// <param name="aggressor">Unit that performed the attack</param>
        /// <param name="damage">Amount of damge that the attack caused</param>
        /// <returns>Amount of damage that the unit has taken</returns>        
        protected virtual int Defend(Unit aggressor, int damage)
        {
            return Mathf.Clamp(damage - DefenceFactor, 1, damage);
        }
        /// <summary>
        /// Method callef after unit performed defence.
        /// </summary>
        protected virtual void DefenceActionPerformed() { }

        public int DryAttack(Unit other)
        {
            int damage = DealDamage(other).Damage;
            int realDamage = other.Defend(this, damage);

            return realDamage;
        }

        /// <summary>
        /// Handler method for moving the unit.
        /// </summary>
        /// <param name="destinationCell">Cell to move the unit to</param>
        /// <param name="path">A list of cells, path from source to destination cell</param>
        public virtual IEnumerator Move(Cell destinationCell, IList<Cell> path)
        {
            var totalMovementCost = path.Sum(h => h.MovementCost);
            MovementPoints -= totalMovementCost;

            Cell.IsTaken = false;
            Cell.CurrentUnits.Remove(this);
            Cell = destinationCell;
            destinationCell.IsTaken = true;
            destinationCell.CurrentUnits.Add(this);

            if (MovementAnimationSpeed > 0)
            {
                yield return MovementAnimation(path);
            }
            else
            {
                transform.position = Cell.transform.position;
                OnMoveFinished();
            }

            if (UnitMoved != null)
            {
                UnitMoved.Invoke(this, new MovementEventArgs(Cell, destinationCell, path, this));
            }
        }
        protected virtual IEnumerator MovementAnimation(IList<Cell> path)
        {
            for (int i = path.Count - 1; i >= 0; i--)
            {
                var currentCell = path[i];
                Vector3 destination_pos = FindObjectOfType<CellGrid>().Is2D ? new Vector3(currentCell.transform.localPosition.x, currentCell.transform.localPosition.y, transform.localPosition.z) : new Vector3(currentCell.transform.localPosition.x, currentCell.transform.localPosition.y, currentCell.transform.localPosition.z);
                while (transform.localPosition != destination_pos)
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, destination_pos, Time.deltaTime * MovementAnimationSpeed);
                    yield return null;
                }
            }

            OnMoveFinished();
        }
        /// <summary>
        /// Method called after movement animation has finished.
        /// </summary>
        protected virtual void OnMoveFinished() { }

        ///<summary>
        /// Method indicates if unit is capable of moving to cell given as parameter.
        /// </summary>
        public virtual bool IsCellMovableTo(Cell cell)
        {
            return !cell.IsTaken;
        }
        /// <summary>
        /// Method indicates if unit is capable of moving through cell given as parameter.
        /// </summary>
        public virtual bool IsCellTraversable(Cell cell)
        {
            return !cell.IsTaken;
        }
        /// <summary>
        /// Method returns all cells that the unit is capable of moving to.
        /// </summary>
        public HashSet<Cell> GetAvailableDestinations(List<Cell> cells)
        {
            if (cachedPaths == null)
            {
                CachePaths(cells);
            }

            var availableDestinations = new HashSet<Cell>();
            foreach (var cell in cells.Where(c => IsCellMovableTo(c)))
            {
                if(cachedPaths.TryGetValue(cell, out var path))
                {
                    var pathCost = path.Sum(c => c.MovementCost);
                    if (pathCost <= MovementPoints)
                    {
                        availableDestinations.Add(cell);
                    }
                }
            }

            return availableDestinations;
        }

        public void CachePaths(List<Cell> cells)
        {
            var edges = GetGraphEdges(cells);
            cachedPaths = _pathfinder.FindAllPaths(edges, Cell);
        }

        public IList<Cell> FindPath(List<Cell> cells, Cell destination)
        {
            if (cachedPaths.TryGetValue(destination, out var path))
            {
                return path;
            }
            return new List<Cell>();
        }
        /// <summary>
        /// Method returns graph representation of cell grid for pathfinding.
        /// </summary>
        protected virtual Dictionary<Cell, Dictionary<Cell, float>> GetGraphEdges(List<Cell> cells)
        {
            Dictionary<Cell, Dictionary<Cell, float>> ret = new Dictionary<Cell, Dictionary<Cell, float>>();
            foreach (var cell in cells)
            {
                if (IsCellTraversable(cell) || cell == Cell)
                {
                    ret[cell] = new Dictionary<Cell, float>();
                    foreach (var neighbour in cell.GetNeighbours(cells))
                    {
                        if(IsCellTraversable(neighbour) || IsCellMovableTo(neighbour))
                        {
                            ret[cell][neighbour] = neighbour.MovementCost;
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Gives visual indication that the unit is under attack.
        /// </summary>
        /// <param name="aggressor">
        /// Unit that is attacking.
        /// </param>
        public virtual void MarkAsDefending(Unit aggressor)
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsDefendingFn?.ForEach(o => o.Apply(this, aggressor));
            }
        }
        /// <summary>
        /// Gives visual indication that the unit is attacking.
        /// </summary>
        /// <param name="target">
        /// Unit that is under attack.
        /// </param>
        public virtual void MarkAsAttacking(Unit target)
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsAttackingFn?.ForEach(o => o.Apply(this, target));
            }
        }
        /// <summary>
        /// Gives visual indication that the unit is destroyed. It gets called right before the unit game object is
        /// destroyed.
        /// </summary>
        public virtual void MarkAsDestroyed()
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsDestroyedFn?.ForEach(o => o.Apply(this, null));
            }
        }

        /// <summary>
        /// Method marks unit as current players unit.
        /// </summary>
        public virtual void MarkAsFriendly()
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsFriendlyFn?.ForEach(o => o.Apply(this, null));
            }
        }
        /// <summary>
        /// Method mark units to indicate user that the unit is in range and can be attacked.
        /// </summary>
        public virtual void MarkAsReachableEnemy()
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsReachableEnemyFn?.ForEach(o => o.Apply(this, null));
            }
        }
        /// <summary>
        /// Method marks unit as currently selected, to distinguish it from other units.
        /// </summary>
        public virtual void MarkAsSelected()
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsSelectedFn?.ForEach(o => o.Apply(this, null));
            }
        }
        /// <summary>
        /// Method marks unit to indicate user that he can't do anything more with it this turn.
        /// </summary>
        public virtual void MarkAsFinished()
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.MarkAsFinishedFn?.ForEach(o => o.Apply(this, null));
            }
        }
        /// <summary>
        /// Method returns the unit to its base appearance
        /// </summary>
        public virtual void UnMark()
        {
            if (UnitHighlighterAggregator != null)
            {
                UnitHighlighterAggregator.UnMarkFn?.ForEach(o => o.Apply(this, null));
            }
        }
        public virtual void SetColor(Color color) { }

        [ExecuteInEditMode]
        public void OnDestroy()
        {
            #if UNITY_EDITOR
            if (Cell != null && !Application.isPlaying)
            {
                Cell.IsTaken = false;
                UnityEditor.EditorUtility.SetDirty(Cell);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            #endif
        }

        private void Reset()
        {
            if (GetComponent<Abilities.AttackAbility>() == null)
            {
                gameObject.AddComponent<Abilities.AttackAbility>();
            }
            if (GetComponent<Abilities.MoveAbility>() == null)
            {
                gameObject.AddComponent<Abilities.MoveAbility>();
            }
            if (GetComponent<Abilities.AttackRangeHighlightAbility>() == null)
            {
                gameObject.AddComponent<Abilities.AttackRangeHighlightAbility>();
            }

            GameObject brain = new GameObject("Brain");
            brain.transform.parent = transform;

            brain.AddComponent<MoveToPositionAIAction>();
            brain.AddComponent<AttackAIAction>();

            brain.AddComponent<DamageCellEvaluator>();
            brain.AddComponent<DamageUnitEvaluator>();
        }
    }

    public class AttackAction
    {
        public readonly int Damage;
        public readonly float ActionCost;

        public AttackAction(int damage, float actionCost)
        {
            Damage = damage;
            ActionCost = actionCost;
        }
    }

    public class MovementEventArgs : EventArgs
    {
        public Cell OriginCell;
        public Cell DestinationCell;
        public IList<Cell> Path;
        public Unit Unit;

        public MovementEventArgs(Cell sourceCell, Cell destinationCell, IList<Cell> path, Unit unit)
        {
            OriginCell = sourceCell;
            DestinationCell = destinationCell;
            Path = path;
            Unit = unit;
        }
    }
    public class AttackEventArgs : EventArgs
    {
        public Unit Attacker;
        public Unit Defender;

        public int Damage;

        public AttackEventArgs(Unit attacker, Unit defender, int damage)
        {
            Attacker = attacker;
            Defender = defender;

            Damage = damage;
        }
    }
    public class UnitCreatedEventArgs : EventArgs
    {
        public Transform unit;

        public UnitCreatedEventArgs(Transform unit)
        {
            this.unit = unit;
        }
    }

    public class AbilityAddedEventArgs : EventArgs
    {
        public readonly Ability ability;

        public AbilityAddedEventArgs(Ability ability)
        {
            this.ability = ability;
        }
    }
}
