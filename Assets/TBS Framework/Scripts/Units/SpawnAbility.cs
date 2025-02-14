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

namespace TbsFramework.Units
{
    public class SpawnAbility : Ability
    {
        public List<GameObject> Prefabs;
        public List<GameObject> SpecialPrefabs;
        public int limitUnits;
        
        [HideInInspector]
        public GameObject SelectedPrefab;

        public GameObject UnitButton;
        public GameObject UnitPanel;
        public GameObject InfoPanel;
        public Text MoneyAmountText;
        public Text limitUnitsNumberText;
        public GameObject StatPanel;
        public GameObject StatCard;
        public GameObject UnitNameCard;
        public Sprite AttackSprite;
        public Sprite DefenceSprite;
        public Sprite RangeSprite;
        public Sprite MovementSprite;
        public Sprite HitpointsSprite;

        public event EventHandler UnitSpawned;

        private Unit SpawnedUnit;
        private List<GameObject> UnitButtons = new List<GameObject>();
        private List<GameObject> StatDisplays = new List<GameObject>();

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (!UnitReference.Cell.IsTaken && FindObjectOfType<EconomyController>().GetValue(GetComponent<Unit>().PlayerNumber) >= SelectedPrefab.GetComponent<Price>().Value)
            {
                FindObjectOfType<EconomyController>().UpdateValue(GetComponent<Unit>().PlayerNumber, SelectedPrefab.GetComponent<Price>().Value * (-1));
                MoneyAmountText.text = FindObjectOfType<EconomyController>().GetValue(GetComponent<Unit>().PlayerNumber).ToString();

                var unitGO = Instantiate(SelectedPrefab);
                SpawnedUnit = unitGO.GetComponent<Unit>();

                var player = FindObjectOfType<CellGrid>().Players.Find(p => p.PlayerNumber == GetComponent<Unit>().PlayerNumber);
                var spriteRenderer = SpawnedUnit.GetComponent<SpriteRenderer>();
                switch (GetComponent<Unit>().PlayerNumber)
                {
                    case 0:
                        Debug.Log("Player 0");
                        spriteRenderer.sprite = unitGO.GetComponent<ESUnit>().Player1Sprite;
                        break;
                    case 1:
                        Debug.Log("Player 1");
                        spriteRenderer.sprite = unitGO.GetComponent<ESUnit>().Player2Sprite;
                        break;
                    // Add more cases for additional players
                    default:
                        Debug.Log("Default");
                        spriteRenderer.sprite = unitGO.GetComponent<ESUnit>().DefaultSprite;
                        break;
                }

                cellGrid.AddUnit(SpawnedUnit.transform, UnitReference.Cell, cellGrid.CurrentPlayer);
                SpawnedUnit.OnTurnStart();

                if (UnitSpawned != null)
                {
                    UnitSpawned.Invoke(unitGO, EventArgs.Empty);
                    SpawnedUnit.GetComponent<Unit>().SetState(new UnitStateMarkedAsFinished(SpawnedUnit.GetComponent<Unit>()));
                }
            }

            yield return base.Act(cellGrid, isNetworkInvoked);
        }

        public override void Display(CellGrid cellGrid)
        {
            MoneyAmountText.text = FindObjectOfType<EconomyController>().GetValue(GetComponent<Unit>().PlayerNumber).ToString();
            int currentUnitCount = cellGrid.Units.Count(u => u.PlayerNumber == GetComponent<Unit>().PlayerNumber && !(u as ESUnit).isStructure && u.isActiveAndEnabled);
            limitUnitsNumberText.text = $"{currentUnitCount}/{limitUnits}";
            
            for (int i = 0; i < Prefabs.Count; i++)
            {
                var UnitPrefab = Prefabs[i];

                var unitButton = Instantiate(UnitButton, UnitButton.transform.parent);
                unitButton.GetComponent<Button>().interactable = UnitPrefab.GetComponent<Price>().Value <= FindObjectOfType<EconomyController>().GetValue(GetComponent<Unit>().PlayerNumber) && (currentUnitCount < limitUnits);
                unitButton.GetComponentInChildren<Button>().onClick.AddListener(() => ActWrapper(UnitPrefab, cellGrid));
                unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = UnitPrefab.GetComponent<SpriteRenderer>().sprite;
                unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = UnitPrefab.GetComponent<ESUnit>().UnitName;
                unitButton.GetComponent<Button>().transform.Find("PriceText").GetComponent<Text>().text = UnitPrefab.GetComponent<Price>().Value.ToString();

                var hoverScript = unitButton.AddComponent<BuyButtonHover>();
                hoverScript.Initialize(UnitPrefab, this);

                unitButton.SetActive(true);
                UnitButtons.Add(unitButton);
            }

            for (int i = 0; i < SpecialPrefabs.Count; i++)
            {
                var UnitPrefab = SpecialPrefabs[i];

                var unitButton = Instantiate(UnitButton, UnitButton.transform.parent);
                unitButton.GetComponent<Button>().interactable = (UnitPrefab.GetComponent<Price>().Value <= FindObjectOfType<EconomyController>().GetValue(GetComponent<Unit>().PlayerNumber)) && (currentUnitCount < limitUnits);
                unitButton.GetComponentInChildren<Button>().onClick.AddListener(() => ActWrapper(UnitPrefab, cellGrid));
                unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = UnitPrefab.GetComponent<SpriteRenderer>().sprite;
                unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = UnitPrefab.GetComponent<ESUnit>().UnitName;
                unitButton.GetComponent<Button>().transform.Find("PriceText").GetComponent<Text>().text = UnitPrefab.GetComponent<Price>().Value.ToString();

                var hoverScript = unitButton.AddComponent<BuyButtonHover>();
                hoverScript.Initialize(UnitPrefab, this);

                if (cellGrid.Units.Exists(u => u.PlayerNumber == cellGrid.CurrentPlayer.PlayerNumber 
                                            && u.GetComponent<ESUnit>().UnitUnlock == UnitPrefab.GetComponent<ESUnit>().UnitName))
                {
                    unitButton.SetActive(true);
                }
                else
                {
                    unitButton.SetActive(false);
                }
                UnitButtons.Add(unitButton);
            }

            UnitPanel.SetActive(true);
            InfoPanel.SetActive(true);
        }

        public void ShowStats(GameObject unitPrefab)
        {
            var unit = unitPrefab.GetComponent<ESUnit>();

            var unitCard = Instantiate(UnitNameCard, UnitNameCard.transform.parent);
            unitCard.transform.GetChild(0).GetComponent<Image>().sprite = unit.GetComponent<SpriteRenderer>().sprite;
            unitCard.transform.GetChild(1).GetComponent<Text>().text = unit.UnitName;
            unitCard.SetActive(true);
            StatDisplays.Add(unitCard);

            var healthCard = Instantiate(StatCard, StatCard.transform.parent);
            healthCard.transform.GetChild(0).GetComponent<Image>().sprite = HitpointsSprite;
            healthCard.transform.GetChild(1).GetComponent<Text>().text = "Health: " + unit.HitPoints.ToString();
            healthCard.SetActive(true);
            StatDisplays.Add(healthCard);

            var attackCard = Instantiate(StatCard, StatCard.transform.parent);
            attackCard.transform.GetChild(0).GetComponent<Image>().sprite = AttackSprite;
            attackCard.transform.GetChild(1).GetComponent<Text>().text = "Attack: " + unit.AttackFactor.ToString();
            attackCard.SetActive(true);
            StatDisplays.Add(attackCard);

            var defenceCard = Instantiate(StatCard, StatCard.transform.parent);
            defenceCard.transform.GetChild(0).GetComponent<Image>().sprite = DefenceSprite;
            defenceCard.transform.GetChild(1).GetComponent<Text>().text = "Defence: " + unit.DefenceFactor.ToString();
            defenceCard.SetActive(true);
            StatDisplays.Add(defenceCard);

            var rangeCard = Instantiate(StatCard, StatCard.transform.parent);
            rangeCard.transform.GetChild(0).GetComponent<Image>().sprite = RangeSprite;
            rangeCard.transform.GetChild(1).GetComponent<Text>().text = "Range: " + unit.AttackRange.ToString();
            rangeCard.SetActive(true);
            StatDisplays.Add(rangeCard);

            var moveCard = Instantiate(StatCard, StatCard.transform.parent);
            moveCard.transform.GetChild(0).GetComponent<Image>().sprite = MovementSprite;
            moveCard.transform.GetChild(1).GetComponent<Text>().text = "Moves: " + unit.MovementPoints.ToString();
            moveCard.SetActive(true);
            StatDisplays.Add(moveCard);

            StatPanel.SetActive(true);
        }

        public void HideStats()
        {
            foreach (var card in StatDisplays)
            {
                Destroy(card);
            }
            StatDisplays.Clear();
            StatPanel.SetActive(false);
        }

        void ActWrapper(GameObject prefab, CellGrid cellGrid)
        {
            SelectedPrefab = prefab;
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

        public override void OnTurnEnd(CellGrid cellGrid)
        {
            if (SpawnedUnit != null)
            {
                SpawnedUnit.GetComponent<Unit>().SetState(new UnitStateNormal(SpawnedUnit.GetComponent<Unit>()));
            }
            SpawnedUnit = null;
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            foreach (var button in UnitButtons)
            {
                Destroy(button);
            }
            UnitButtons.Clear();
            UnitPanel.SetActive(false);
            InfoPanel.SetActive(false);

            if (StatDisplays.Count > 0)
            {
                HideStats();
            }
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return true;
        }

        public override IDictionary<string, string> Encapsulate()
        {
            var selectedPrefabIndex = Prefabs.IndexOf(SelectedPrefab);

            Dictionary<string, string> actionParams = new Dictionary<string, string>();
            actionParams.Add("prefab_index", selectedPrefabIndex.ToString());

            return actionParams;
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked)
        {
            var selectedPrefabIndex = int.Parse(actionParams["prefab_index"]);
            SelectedPrefab = Prefabs[selectedPrefabIndex];

            yield return StartCoroutine(Execute(cellGrid, _ => cellGrid.cellGridState = new CellGridStateRemotePlayerTurn(cellGrid), _ => { }, isNetworkInvoked));
        }
    }
}