using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using TbsFramework.Units.UnitStates;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeAbility : Ability
{
    public int upgradeCost;
    public GameObject prefabToChange;
    public GameObject upgradeButton;
    public Text MoneyAmountText;
    public GameObject upgradePanel;
    public event EventHandler UnitUpgrade;
    private Unit newUnit;

    public override void Display(CellGrid cellGrid)
    {
        if (upgradeButton != null)
        {
            upgradeButton.GetComponent<Button>().onClick.AddListener(() => ActWrapper(prefabToChange, cellGrid));
            upgradeButton.GetComponent<Button>().transform.Find("PriceText").GetComponent<Text>().text = upgradeCost.ToString();
            upgradeButton.SetActive(true);
            upgradePanel.SetActive(true);
        }
    }

    public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
    {
        if (prefabToChange != null)
        {
            var economyController = FindObjectOfType<EconomyController>();
            if (economyController.GetValue(cellGrid.CurrentPlayerNumber) >= upgradeCost)
            {
                economyController.UpdateValue(cellGrid.CurrentPlayerNumber, -upgradeCost);
                MoneyAmountText.text = economyController.GetValue(cellGrid.CurrentPlayerNumber).ToString();

                var unitGO = Instantiate(prefabToChange);
                newUnit = unitGO.GetComponent<Unit>();

                newUnit.transform.SetParent(UnitReference.transform.parent);

                cellGrid.AddUnit(newUnit.transform, UnitReference.Cell, cellGrid.CurrentPlayer);
                newUnit.OnTurnStart();

                 var structureCaptureConditions = cellGrid.GetComponents<StructureCaptureCondition>();
                foreach (var condition in structureCaptureConditions)
                {
                    if (condition.TargetPlayerNumber != cellGrid.CurrentPlayerNumber)
                    {
                        condition.StructureToCapture = newUnit;
                    }
                }

                if (UnitUpgrade != null)
                {
                    UnitUpgrade.Invoke(unitGO, EventArgs.Empty);
                    newUnit.GetComponent<Unit>().SetState(new UnitStateMarkedAsFinished(newUnit.GetComponent<Unit>()));
                }

                var renderer = UnitReference.gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
                else
                {
                    var renderers = UnitReference.gameObject.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        rend.enabled = false;
                    }
                }
                UnitReference.Cell.CurrentUnits.Remove(UnitReference);
            }
        }
        yield return base.Act(cellGrid, isNetworkInvoked);
    }

    void ActWrapper(GameObject prefab, CellGrid cellGrid)
    {
        prefabToChange = prefab;
        StartCoroutine(Execute(cellGrid,
                _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                _ => cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid)));
    }

    public override void OnUnitClicked(Unit unit, CellGrid cellGrid)
    {
        if (cellGrid.GetCurrentPlayerUnits().Contains(unit))
        {
            cellGrid.cellGridState = new CellGridStateAbilitySelected(cellGrid, unit, unit.GetComponents<Ability>().ToList());
        }
    }

    public override void OnCellClicked(Cell cell, CellGrid cellGrid)
    {
        cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid);
    }

    public override void CleanUp(CellGrid cellGrid)
    {
        upgradeButton.SetActive(false);
        upgradePanel.SetActive(false);
    }

    public override bool CanPerform(CellGrid cellGrid)
    {
        return true;
    }

    public override void OnTurnEnd(CellGrid cellGrid)
    {
        if (newUnit != null)
        {
            newUnit.GetComponent<Unit>().SetState(new UnitStateNormal(newUnit.GetComponent<Unit>()));
            UnitReference.gameObject.SetActive(false);
        }
        newUnit = null;
    }
    public override IDictionary<string, string> Encapsulate()
    {
        return new Dictionary<string, string>();
    }
    public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked)
    {
        yield return StartCoroutine(Execute(cellGrid, _ => cellGrid.cellGridState = new CellGridStateRemotePlayerTurn(cellGrid), _ => { }, isNetworkInvoked));
    }
}