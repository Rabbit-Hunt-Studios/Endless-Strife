using System.Collections;
using System.Linq;
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
        public Cell unmergeSquare { get; set; }
        public GameObject UnmergeButton;
        public GameObject UnmergePanel;
        public Sprite EmptyDefault;
        private List<GameObject> UnmergeButtons = new List<GameObject>();

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (UnitReference.ActionPoints > 0 && unmergeSquare != null && unitToUnmerge != null)
            {
                UnitReference.GetComponent<ESUnit>().TotalHitPoints -= (int)unitToUnmerge.GetComponent<MergeStats>().HitPoints;
                unitToUnmerge.GetComponent<ESUnit>().HitPoints = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().HitPoints * (1 - unitToUnmerge.GetComponent<MergeStats>().UnmergePenalty)));
                UnitReference.GetComponent<ESUnit>().HitPoints = (int)System.Math.Round((float)(UnitReference.GetComponent<ESUnit>().HitPoints * unitToUnmerge.GetComponent<MergeStats>().UnmergePenalty));

                UnitReference.GetComponent<ESUnit>().AttackFactor -= (int)unitToUnmerge.GetComponent<MergeStats>().Attack;

                UnitReference.GetComponent<ESUnit>().DefenceFactor -= (int)unitToUnmerge.GetComponent<MergeStats>().Defence;

                UnitReference.GetComponent<ESUnit>().TotalMovementPoints -= (int)unitToUnmerge.GetComponent<MergeStats>().Movement;

                UnitReference.GetComponent<ESUnit>().AttackRange -= (int)unitToUnmerge.GetComponent<MergeStats>().AttackRange;

                var tmp = unitToUnmerge.GetComponent<ESUnit>().MovementAnimationSpeed;
                unitToUnmerge.GetComponent<ESUnit>().MovementAnimationSpeed = 1000;

                unitToUnmerge.CachePaths(cellGrid.Cells);
                var path = unitToUnmerge.FindPath(cellGrid.Cells, unmergeSquare);
                yield return unitToUnmerge.Move(unmergeSquare, path);

                unitToUnmerge.GetComponent<ESUnit>().MovementAnimationSpeed = tmp;
                unitToUnmerge.gameObject.SetActive(true);

                UnitReference.GetComponent<MergeAbility>().mergedUnits.Remove(unitToUnmerge);
                unitToUnmerge = null;
                unmergeSquare = null;

                UnitReference.GetComponent<ESUnit>().ActionPoints = 0;
            }
            yield return base.Act(cellGrid, isNetworkInvoked);
        }
        public override void Display(CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0)
            {
                var unmergeButton = Instantiate(UnmergeButton, UnmergeButton.transform.parent);
                if (unitToUnmerge != null && unmergeSquare != null)
                {
                    unmergeButton.GetComponent<Button>().interactable = true;
                    unmergeButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = unitToUnmerge.GetComponent<SpriteRenderer>().sprite;
                    unmergeButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = unitToUnmerge.GetComponent<ESUnit>().UnitName;
                    unmergeButton.GetComponentInChildren<Button>().onClick.AddListener(() => ActWrapper(cellGrid));
                }
                else if (unitToUnmerge != null && unmergeSquare == null)
                {
                    unmergeButton.GetComponent<Button>().interactable = false;
                    unmergeButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = EmptyDefault;
                    unmergeButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = "No squares to unmerge to";
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
            // Debug.Log(unmergeSquare);
            Debug.Log(unitToUnmerge);
        }

        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            unmergeSquare = GetAvailableSpace(cellGrid);
            Debug.Log(UnitReference.GetComponent<MergeAbility>().mergedUnits.Count);
            unitToUnmerge = UnitReference.GetComponent<MergeAbility>().mergedUnits.Count == 0 ? null : UnitReference.GetComponent<MergeAbility>().mergedUnits.LastOrDefault();
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            foreach (var button in UnmergeButtons)
            {
                Destroy(button);
            }
            UnmergeButtons.Clear();
            UnmergePanel.SetActive(false);
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return UnitReference.ActionPoints > 0 && GetAvailableSpace(cellGrid) != null && unitToUnmerge != null;
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

        private Cell GetAvailableSpace(CellGrid cellGrid)
        {
            var unit_x = UnitReference.transform.localPosition.x;
            var unit_y = UnitReference.transform.localPosition.y;

            List<UnityEngine.Vector3> directions = new List<UnityEngine.Vector3>();

            if (UnitReference.GetComponent<ESUnit>().PlayerNumber == 0)
            {
                directions.Add(new UnityEngine.Vector3(unit_x, (unit_y - 0.16f), 0));
                directions.Add(new UnityEngine.Vector3(unit_x, (unit_y + 0.16f), 0));
                directions.Add(new UnityEngine.Vector3((unit_x - 0.16f), unit_y, 0));
                directions.Add(new UnityEngine.Vector3((unit_x + 0.16f), unit_y, 0));
            }
            else if (UnitReference.GetComponent<ESUnit>().PlayerNumber == 1)
            {
                directions.Add(new UnityEngine.Vector3(unit_x, (unit_y + 0.16f), 0));
                directions.Add(new UnityEngine.Vector3(unit_x, (unit_y - 0.16f), 0));
                directions.Add(new UnityEngine.Vector3((unit_x + 0.16f), unit_y, 0));
                directions.Add(new UnityEngine.Vector3((unit_x - 0.16f), unit_y, 0));
            }

            foreach (var direction in directions)
            {
                var tmp = cellGrid.Cells.Find(c => c.transform.localPosition.Equals(direction) && !c.IsTaken);
                if (tmp != null)
                {
                    return tmp;
                }
            }
            Debug.Log("No squares found");
            return null;
        }
    }
}
