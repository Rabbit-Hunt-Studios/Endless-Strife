using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class IncomeAbility : Ability
{
    public int Amount;

    private bool start = true;

    public override void OnTurnStart(CellGrid cellGrid)
    {
        var economyController = FindObjectOfType<EconomyController>();
        economyController.UpdateValue(GetComponent<Unit>().PlayerNumber, Amount);
        if (this.isActiveAndEnabled)
        {
            StartCoroutine(UpdateValues(cellGrid));
        }
    }

    private IEnumerator UpdateValues(CellGrid cellGrid)
    {
        yield return new WaitForSeconds(1);

        SaveAbility.PlayerData toAdd = new SaveAbility.PlayerData();
        toAdd.totalMoney = Amount;
        if (start)
        {
            toAdd.totalMoney += 200;
            start = false;
        }
        cellGrid.CurrentPlayer.GetComponent<SaveAbility>().UpdateValues(toAdd);
    }
}


