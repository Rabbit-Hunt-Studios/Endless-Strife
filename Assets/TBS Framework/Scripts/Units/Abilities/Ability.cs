using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using UnityEngine;

namespace TbsFramework.Units.Abilities
{
    public abstract class Ability : MonoBehaviour
    {
        public int AbilityID { get; set; }
        public Unit UnitReference { get; internal set; }

        public event EventHandler<(bool isNetworkInvoked, IDictionary<string, string> actionParams)> AbilityUsed;


        public IEnumerator Execute(CellGrid cellGrid, Action<CellGrid> preAction, Action<CellGrid> postAction, bool isNetworkInvoked = false)
        {
            if (AbilityUsed != null)
            {
                var actionParams = Encapsulate();
                actionParams.Add("is_network_invoked", isNetworkInvoked.ToString());
                actionParams.Add("unit_id", UnitReference.UnitID.ToString());
                actionParams.Add("ability_id", AbilityID.ToString());

                AbilityUsed.Invoke(this, (isNetworkInvoked, actionParams));
            }
            
            yield return StartCoroutine(Act(cellGrid, preAction, postAction, isNetworkInvoked));
        }

        public IEnumerator HumanExecute(CellGrid cellGrid)
        {
            yield return Execute(cellGrid,
                _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                _ => cellGrid.cellGridState = new CellGridStateAbilitySelected(cellGrid, UnitReference, UnitReference.GetComponents<Ability>().ToList()));
        }

        public IEnumerator RemoteExecute(CellGrid cellGrid)
        {
            yield return StartCoroutine(Execute(cellGrid,
                _ => cellGrid.cellGridState = new CellGridStateRemotePlayerTurn(cellGrid),
                _ => { },
                true));

        }

        public IEnumerator AIExecute(CellGrid cellGrid)
        {
            yield return Execute(cellGrid,
                _ => { },
                _ => OnAbilityDeselected(cellGrid));
        }

        public virtual IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            yield return null;
        }

        private IEnumerator Act(CellGrid cellGrid, Action<CellGrid> preAction, Action<CellGrid> postAction, bool isNetworkInvoked = false)
        {
            preAction(cellGrid);
            yield return StartCoroutine(Act(cellGrid, isNetworkInvoked));
            postAction(cellGrid);

            yield return null;
        }

        public virtual void Initialize() { }

        public virtual void OnUnitClicked(Unit unit, CellGrid cellGrid) { }
        public virtual void OnUnitHighlighted(Unit unit, CellGrid cellGrid) { }
        public virtual void OnUnitDehighlighted(Unit unit, CellGrid cellGrid) { }
        public virtual void OnUnitDestroyed(CellGrid cellGrid) { }
        public virtual void OnCellClicked(Cell cell, CellGrid cellGrid) { }
        public virtual void OnCellSelected(Cell cell, CellGrid cellGrid) { }
        public virtual void OnCellDeselected(Cell cell, CellGrid cellGrid) { }
        public virtual void Display(CellGrid cellGrid) { }
        public virtual void CleanUp(CellGrid cellGrid) { }

        public virtual void OnAbilitySelected(CellGrid cellGrid) { }
        public virtual void OnAbilityDeselected(CellGrid cellGrid) { }
        public virtual void OnTurnStart(CellGrid cellGrid) { }
        public virtual void OnTurnEnd(CellGrid cellGrid) { }

        public virtual bool CanPerform(CellGrid cellGrid) { return false; }

        /// <summary>
        /// Encapsulates the ability's parameters into a dictionary for network transmission.
        /// This method should be overridden to serialize the ability's data into a format
        /// that can be sent and understood by both the client and server during a networked game session.
        /// </summary>
        /// <returns>A dictionary with string keys and values containing the serialized ability parameters.</returns>
        /// <exception cref="NotImplementedException">Throws an exception if the method is not implemented.</exception>
        public virtual IDictionary<string, string> Encapsulate() { throw new NotImplementedException(); }

        /// <summary>
        /// Applies the ability's effect based on the parameters received, usually from a network message.
        /// This method should be overridden to deserialize the parameters and perform the ability's action.
        /// This is used during a networked game to apply effects that were initiated by another player.
        /// </summary>
        /// <param name="cellGrid">The cell grid on which the ability is to be applied.</param>
        /// <param name="actionParams">The parameters required to execute the ability, typically received from a network message.</param>
        /// <param name="isNetworkInvoked">Indicates whether the ability is being invoked as part of a network operation.</param>
        /// <returns>An IEnumerator for coroutine management, allowing the method to yield execution until the ability's effect is complete.</returns>
        /// <exception cref="NotImplementedException">Throws an exception if the method is not implemented.</exception>
        public virtual IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked=true) { throw new NotImplementedException();  }
    }
}
