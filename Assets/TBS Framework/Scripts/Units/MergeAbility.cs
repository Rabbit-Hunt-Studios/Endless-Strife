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
        public GameObject UnitPanel;
        public Sprite EmptyDefault;
        public List<Unit> mergedUnits = new List<Unit>();
        private List<GameObject> MergeButtons = new List<GameObject>();

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (UnitReference.ActionPoints > 0 && availableMerges.Contains(unitToMerge))
            {
                UnitReference.GetComponent<ESUnit>().HitPoints = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().HitPoints + unitToMerge.GetComponent<ESUnit>().HitPoints) / 2, 0);
                UnitReference.GetComponent<ESUnit>().AttackFactor = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().AttackFactor + unitToMerge.GetComponent<ESUnit>().AttackFactor) / 2, 0);
                UnitReference.GetComponent<ESUnit>().DefenceFactor = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().DefenceFactor + unitToMerge.GetComponent<ESUnit>().DefenceFactor) / 2, 0);
                UnitReference.GetComponent<ESUnit>().MovementPoints = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().MovementPoints + unitToMerge.GetComponent<ESUnit>().MovementPoints) / 2, 0);
                UnitReference.GetComponent<ESUnit>().AttackRange = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().AttackRange + unitToMerge.GetComponent<ESUnit>().AttackRange) / 2, 0);

                var takenCell = cellGrid.Cells.Find(c => (c.transform.localPosition.x.Equals(unitToMerge.transform.localPosition.x) && c.transform.localPosition.y.Equals(unitToMerge.transform.localPosition.y)));
                takenCell.IsTaken = false;

                unitToMerge.gameObject.SetActive(false);
                UnitReference.Cell.CurrentUnits.Remove(unitToMerge);
            }
            yield return base.Act(cellGrid, isNetworkInvoked);
        }
        public override void Display(CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0)
            {
                if (availableMerges.Count > 0)
                {
                    foreach (var unit in availableMerges)
                    {
                        unit.MarkAsSelected();

                        var unitButton = Instantiate(MergeButton, MergeButton.transform.parent);
                        unitButton.GetComponent<Button>().interactable = true;
                        unitButton.GetComponentInChildren<Button>().onClick.AddListener(() => ActWrapper(unit, cellGrid));

                        unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = unit.GetComponent<SpriteRenderer>().sprite;
                        unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = unit.GetComponent<ESUnit>().UnitName;

                        unitButton.SetActive(true);
                        MergeButtons.Add(unitButton);
                    }
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
                UnitPanel.SetActive(true);
            }
        }

        void ActWrapper(Unit unit, CellGrid cellGrid)
        {
            unitToMerge = unit;
            StartCoroutine(Execute(cellGrid,
                    _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                    _ => cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid)));
            Debug.Log(unitToMerge.GetComponent<ESUnit>().UnitName);
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
            UnitPanel.SetActive(false);
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return UnitReference.ActionPoints > 0 && GetAvailableMerges(cellGrid).Count > 0;
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
            if (unitToMerge != null)
            {
                mergedUnits.Add(unitToMerge);
            }
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
