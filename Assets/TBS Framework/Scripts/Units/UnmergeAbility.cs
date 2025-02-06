using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TbsFramework.Units;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units.Abilities;


namespace TbsFramework.Units
{
    public class UnmergeAbility : Ability
    {
        public Unit unitToUnmerge { get; set; }
        public Square unmergeSquare { get; set; }
        public GameObject UnmergeButton;
        public GameObject UnmergePanel;
        public Sprite EmptyDefault;
        private List<GameObject> UnmergeButtons = new List<GameObject>();

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (UnitReference.ActionPoints > 0 && unmergeSquare != null)
            {
                Debug.Log("unmerge TBA");
            }
            yield return base.Act(cellGrid, isNetworkInvoked);
        }
        public override void Display(CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0)
            {
                var unmergeButton = Instantiate(UnmergeButton, UnmergeButton.transform.parent);
                if (unitToUnmerge != null)
                {
                    unmergeButton.GetComponent<Button>().interactable = true;
                    unmergeButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = unitToUnmerge.GetComponent<SpriteRenderer>().sprite;
                    unmergeButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = unitToUnmerge.GetComponent<ESUnit>().UnitName;
                    unmergeButton.GetComponentInChildren<Button>().onClick.AddListener(() => ActWrapper(cellGrid));
                }
                else
                {
                unmergeButton.GetComponent<Button>().interactable = false;
                unmergeButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = EmptyDefault;
                unmergeButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = "No units to unmerge";
                }
                unmergeButton.SetActive(true);
                UnmergeButtons.Add(unmergeButton);

                UnmergePanel.SetActive(true);
            }
        }

        void ActWrapper(CellGrid cellGrid)
        {
            StartCoroutine(Execute(cellGrid,
                    _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                    _ => cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid)));
            Debug.Log(unmergeSquare);
        }

        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            var spaces = GetAvailableSpaces(cellGrid);
            unmergeSquare = (spaces.Count == 0 ? null : spaces[0]);
            
            unitToUnmerge = UnitReference.GetComponent<MergeAbility>().mergedUnits.Count == 0 ? null : UnitReference.GetComponent<MergeAbility>().mergedUnits[0];
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            foreach (var button in UnmergeButtons)
            {
                Destroy(button);
            }
            UnmergePanel.SetActive(false);
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return UnitReference.ActionPoints > 0 && GetAvailableSpaces(cellGrid).Count > 0 && unitToUnmerge != null;
        }

        public override IDictionary<string, string> Encapsulate()
        {
            var actionParams = new Dictionary<string, string>();
            return actionParams;
        }
        public override void OnTurnEnd(CellGrid cellGrid)
        {
            unitToUnmerge = null;
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked = false)
        {
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }

        private List<Square> GetAvailableSpaces(CellGrid cellGrid) 
        {
            // var unitsFriendly = cellGrid.Units.FindAll(u => u.PlayerNumber == cellGrid.CurrentPlayer.PlayerNumber);
            // var result = new HashSet<Unit>();
            // var unit_x = System.Math.Round(UnitReference.transform.localPosition.x, 2);
            // var unit_y = System.Math.Round(UnitReference.transform.localPosition.y, 2);
            
            // foreach (var unit in unitsFriendly)
            // {
            //     if(!unit.Equals(UnitReference) && !unit.GetComponent<ESUnit>().isStructure && unit.gameObject.activeSelf)
            //     {
            //         var otherUnit_x = System.Math.Round(unit.transform.localPosition.x, 2);
            //         var otherUnit_y = System.Math.Round(unit.transform.localPosition.y, 2);

            //         if (otherUnit_x.Equals(unit_x) &&
            //             (otherUnit_y.Equals(System.Math.Round(unit_y - 0.16f, 2)) || otherUnit_y.Equals(System.Math.Round(unit_y + 0.16f, 2))))
            //         {
            //             result.Add(unit);
            //         }
            //         else if (otherUnit_y.Equals(unit_y) &&
            //                  (otherUnit_x.Equals(System.Math.Round(unit_x - 0.16f, 2)) || otherUnit_x.Equals(System.Math.Round(unit_x + 0.16f, 2))))
            //         {
            //             result.Add(unit);
            //         }
            //     }
            // }
            // // Debug.Log("found " + result.Count.ToString() + " adjacent");
            // return result;
            return new List<Square>();
        }
    }
}
