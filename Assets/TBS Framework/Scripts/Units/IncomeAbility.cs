using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IncomeAbility : Ability
{
    public int Amount;

    public override void OnTurnStart(CellGrid cellGrid)
    {
        var economyController = FindObjectOfType<EconomyController>();
        economyController.UpdateValue(GetComponent<Unit>().PlayerNumber, Amount);
    }
}


