using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TbsFramework.Units;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units.Abilities;


namespace TbsFramework.Units
{
    public class MergeAbility : Ability
    {
        public Unit unitToMerge { get; set; }
        public HashSet<Unit> availableMerges;
        public GameObject MergeButton;
        public GameObject MergeUI;
        public GameObject StatPanel;
        public GameObject StatCard;
        public Sprite EmptyDefault;
        public List<Unit> mergedUnits = new List<Unit>();
        public int MaxMerges;
        private List<GameObject> MergeButtons = new List<GameObject>();
        private List<GameObject> StatDisplays = new List<GameObject>();

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (UnitReference.ActionPoints > 0 && availableMerges.Contains(unitToMerge))
            {
                UnitReference.GetComponent<ESUnit>().HitPoints = (int)UnitReference.GetComponent<ESUnit>().HitPoints + unitToMerge.GetComponent<MergeStats>().HitPoints;
                UnitReference.GetComponent<ESUnit>().AttackFactor = (int)UnitReference.GetComponent<ESUnit>().AttackFactor + unitToMerge.GetComponent<MergeStats>().Attack;
                UnitReference.GetComponent<ESUnit>().DefenceFactor = (int)UnitReference.GetComponent<ESUnit>().DefenceFactor + unitToMerge.GetComponent<MergeStats>().Defence;
                UnitReference.GetComponent<ESUnit>().MovementPoints = (int)UnitReference.GetComponent<ESUnit>().MovementPoints + unitToMerge.GetComponent<MergeStats>().Movement;
                UnitReference.GetComponent<ESUnit>().AttackRange = (int)UnitReference.GetComponent<ESUnit>().AttackRange + unitToMerge.GetComponent<MergeStats>().AttackRange;

                UnitReference.GetComponent<ESUnit>().TotalMovementPoints = (int)UnitReference.GetComponent<ESUnit>().TotalMovementPoints + unitToMerge.GetComponent<MergeStats>().Movement;
                UnitReference.GetComponent<ESUnit>().TotalHitPoints = (int)UnitReference.GetComponent<ESUnit>().TotalHitPoints + unitToMerge.GetComponent<MergeStats>().HitPoints;

                var takenCell = cellGrid.Cells.Find(c => (c.transform.localPosition.x.Equals(unitToMerge.transform.localPosition.x) && c.transform.localPosition.y.Equals(unitToMerge.transform.localPosition.y)));
                takenCell.IsTaken = false;

                mergedUnits.Add(unitToMerge);

                unitToMerge.gameObject.SetActive(false);

                unitToMerge = null;
            }
            yield return base.Act(cellGrid, isNetworkInvoked);
        }
        public override void Display(CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0)
            {
                if (availableMerges.Count > 0 && mergedUnits.Count < MaxMerges)
                {
                    foreach (var unit in availableMerges)
                    {
                        unit.MarkAsSelected();

                        var unitButton = Instantiate(MergeButton, MergeButton.transform.parent);
                        unitButton.GetComponent<Button>().interactable = true;
                        unitButton.GetComponentInChildren<Button>().onClick.AddListener(() => ActWrapper(unit, cellGrid));

                        unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = unit.GetComponent<SpriteRenderer>().sprite;
                        unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = unit.GetComponent<ESUnit>().UnitName;

                        var hoverScript = unitButton.AddComponent<MergeButtonHover>();
                        hoverScript.Initialize(unit, this);

                        unitButton.SetActive(true);
                        MergeButtons.Add(unitButton);
                    }
                }
                else if (mergedUnits.Count >= MaxMerges)
                {
                    var unitButton = Instantiate(MergeButton, MergeButton.transform.parent);
                    unitButton.GetComponent<Button>().interactable = false;
                    unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = EmptyDefault;
                    unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = "Max Merges Reached";

                    unitButton.SetActive(true);
                    MergeButtons.Add(unitButton);
                }
                else
                {
                    var unitButton = Instantiate(MergeButton, MergeButton.transform.parent);
                    unitButton.GetComponent<Button>().interactable = false;
                    unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = EmptyDefault;
                    unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = "No units to merge";

                    unitButton.SetActive(true);
                    MergeButtons.Add(unitButton);
                }

                MergeUI.transform.GetChild(0).GetComponent<Text>().text = " Merges: " + mergedUnits.Count.ToString() + "/" + MaxMerges.ToString();
                MergeUI.SetActive(true);
            }
        }

        void ActWrapper(Unit unit, CellGrid cellGrid)
        {
            unitToMerge = unit;
            StartCoroutine(Execute(cellGrid,
                    _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                    _ => cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid)));
            // Debug.Log(unitToMerge.GetComponent<ESUnit>().UnitName);
        }

        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            availableMerges = GetAvailableMerges(cellGrid);
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            foreach (var unit in availableMerges)
            {
                unit.UnMark();
            }
            foreach (var button in MergeButtons)
            {
                Destroy(button);
            }
            if (StatDisplays.Count > 0)
            {
                HideMergePreview();
            }
            MergeUI.SetActive(false);
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return UnitReference.ActionPoints > 0 && GetAvailableMerges(cellGrid).Count > 0;
        }

        public void ShowMergePreview(Unit unitToMerge)
        {
            var unit = unitToMerge.GetComponent<MergeStats>();

            var healthCard = Instantiate(StatCard, StatCard.transform.parent);
            healthCard.transform.GetChild(0).GetComponent<Image>().sprite = unitToMerge.GetComponent<StatsDisplayAbility>().HitpointsSprite;;
            healthCard.transform.GetChild(1).GetComponent<Text>().text = "Health: +" + unit.HitPoints.ToString();
            healthCard.SetActive(true);
            StatDisplays.Add(healthCard);

            var attackCard = Instantiate(StatCard, StatCard.transform.parent);
            attackCard.transform.GetChild(0).GetComponent<Image>().sprite = unitToMerge.GetComponent<StatsDisplayAbility>().AttackSprite;
            attackCard.transform.GetChild(1).GetComponent<Text>().text = "Attack: +" + unit.Attack.ToString();
            attackCard.SetActive(true);
            StatDisplays.Add(attackCard);

            var defenceCard = Instantiate(StatCard, StatCard.transform.parent);
            defenceCard.transform.GetChild(0).GetComponent<Image>().sprite = unitToMerge.GetComponent<StatsDisplayAbility>().DefenceSprite;
            defenceCard.transform.GetChild(1).GetComponent<Text>().text = "Defence: +" + unit.Defence.ToString();
            defenceCard.SetActive(true);
            StatDisplays.Add(defenceCard);

            var rangeCard = Instantiate(StatCard, StatCard.transform.parent);
            rangeCard.transform.GetChild(0).GetComponent<Image>().sprite = unitToMerge.GetComponent<StatsDisplayAbility>().RangeSprite;
            rangeCard.transform.GetChild(1).GetComponent<Text>().text = "Range: +" + unit.AttackRange.ToString();
            rangeCard.SetActive(true);
            StatDisplays.Add(rangeCard);

            var moveCard = Instantiate(StatCard, StatCard.transform.parent);
            moveCard.transform.GetChild(0).GetComponent<Image>().sprite = unitToMerge.GetComponent<StatsDisplayAbility>().MovementSprite;
            moveCard.transform.GetChild(1).GetComponent<Text>().text = "Moves: +" + unit.Movement.ToString();
            moveCard.SetActive(true);
            StatDisplays.Add(moveCard);

            StatPanel.SetActive(true);
        }

        public void HideMergePreview()
        {
            foreach (var card in StatDisplays)
            {
                Destroy(card);
            }
            StatDisplays.Clear();
            StatPanel.SetActive(false);
        }

        public override IDictionary<string, string> Encapsulate()
        {
            var actionParams = new Dictionary<string, string>();

            actionParams.Add("unit_x", unitToMerge.transform.localPosition.x.ToString());
            actionParams.Add("unit_y", unitToMerge.transform.localPosition.y.ToString());

            return actionParams;
        }
        public override void OnTurnEnd(CellGrid cellGrid)
        {
            unitToMerge = null;
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked = false)
        {
            var targetUnit = cellGrid.Units.Find(u => u.transform.localPosition.Equals(new UnityEngine.Vector3(float.Parse(actionParams["unit_x"]), float.Parse(actionParams["unit_y"]), -0.01f)));
            unitToMerge = targetUnit;
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }

        private HashSet<Unit> GetAvailableMerges(CellGrid cellGrid) 
        {
            var unitsFriendly = cellGrid.Units.FindAll(u => u.PlayerNumber == cellGrid.CurrentPlayer.PlayerNumber);
            var result = new HashSet<Unit>();
            var unit_x = System.Math.Round(UnitReference.transform.localPosition.x, 2);
            var unit_y = System.Math.Round(UnitReference.transform.localPosition.y, 2);
            
            foreach (var unit in unitsFriendly)
            {
                if(!unit.Equals(UnitReference) && !unit.GetComponent<ESUnit>().isStructure && unit.gameObject.activeSelf)
                {
                    var otherUnit_x = System.Math.Round(unit.transform.localPosition.x, 2);
                    var otherUnit_y = System.Math.Round(unit.transform.localPosition.y, 2);

                    if (otherUnit_x.Equals(unit_x) &&
                        (otherUnit_y.Equals(System.Math.Round(unit_y - 0.16f, 2)) || otherUnit_y.Equals(System.Math.Round(unit_y + 0.16f, 2))))
                    {
                        result.Add(unit);
                    }
                    else if (otherUnit_y.Equals(unit_y) &&
                             (otherUnit_x.Equals(System.Math.Round(unit_x - 0.16f, 2)) || otherUnit_x.Equals(System.Math.Round(unit_x + 0.16f, 2))))
                    {
                        result.Add(unit);
                    }
                }
            }
            // Debug.Log("found " + result.Count.ToString() + " adjacent");
            return result;
        }
    }
}
