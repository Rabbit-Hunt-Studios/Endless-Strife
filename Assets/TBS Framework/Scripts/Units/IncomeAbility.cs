using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IncomeAbility : Ability
{
    public int Amount;
    public Text MoneyPanel;

    public override void OnTurnStart(CellGrid cellGrid)
    {
        var economyController = FindObjectOfType<EconomyController>();
        economyController.UpdateValue(GetComponent<Unit>().PlayerNumber, Amount);
        MoneyPanel.text = economyController.GetValue(GetComponent<Unit>().PlayerNumber).ToString();
    }
}


