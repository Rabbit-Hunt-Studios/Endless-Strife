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
        public Sprite EmptyDefault;
        public List<Unit> mergedUnits = new List<Unit>();
        public int MaxMerges;
        private List<GameObject> MergeButtons = new List<GameObject>();
        private List<GameObject> StatDisplays = new List<GameObject>();
        private Dictionary<string, int> UnitTypes = new Dictionary<string, int>
        {
            {"Empty", -1},
            {"Archer", 0},
            {"Assassin", 1},
            {"AxeMan", 2},
            {"Knight", 3},
            {"Musketeer", 4},
            {"SpearMan", 5},
            {"SwordMan", 6},
            {"Wizard", 7}
        };
        private AudioController audioController;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (UnitReference.ActionPoints > 0 && availableMerges.Contains(unitToMerge))
            {
                audioController = GameObject.Find("AudioController").GetComponent<AudioController>();
                audioController.PlaySFX(audioController.ButtonClick);
                UnitReference.GetComponent<ESUnit>().HitPoints = (int)UnitReference.GetComponent<ESUnit>().HitPoints + unitToMerge.GetComponent<MergeStats>().HitPoints;
                UnitReference.GetComponent<ESUnit>().AttackFactor = (int)UnitReference.GetComponent<ESUnit>().AttackFactor + unitToMerge.GetComponent<MergeStats>().Attack;
                UnitReference.GetComponent<ESUnit>().DefenceFactor = (int)UnitReference.GetComponent<ESUnit>().DefenceFactor + unitToMerge.GetComponent<MergeStats>().Defence;
                UnitReference.GetComponent<ESUnit>().MovementPoints = (int)UnitReference.GetComponent<ESUnit>().MovementPoints + unitToMerge.GetComponent<MergeStats>().Movement;
                UnitReference.GetComponent<ESUnit>().AttackRange = (int)UnitReference.GetComponent<ESUnit>().AttackRange + unitToMerge.GetComponent<MergeStats>().AttackRange;

                UnitReference.GetComponent<ESUnit>().TotalMovementPoints = (int)UnitReference.GetComponent<ESUnit>().TotalMovementPoints + unitToMerge.GetComponent<MergeStats>().Movement;
                UnitReference.GetComponent<ESUnit>().TotalHitPoints = (int)UnitReference.GetComponent<ESUnit>().TotalHitPoints + unitToMerge.GetComponent<MergeStats>().HitPoints;

                if (UnitReference.GetComponent<ESUnit>().DefenceFactor < 0)
                {
                    UnitReference.GetComponent<ESUnit>().DefenceFactor = 0;
                }
                else if (UnitReference.GetComponent<ESUnit>().DefenceFactor > 50)
                {
                    UnitReference.GetComponent<ESUnit>().DefenceFactor = 50;
                }

                if (UnitReference.GetComponent<ESUnit>().MovementPoints < 0)
                {
                    UnitReference.GetComponent<ESUnit>().MovementPoints = 0;
                }
                else if (UnitReference.GetComponent<ESUnit>().MovementPoints > 5)
                {
                    UnitReference.GetComponent<ESUnit>().MovementPoints = 5;
                }

                if (UnitReference.GetComponent<ESUnit>().AttackRange < 1)
                {
                    UnitReference.GetComponent<ESUnit>().AttackRange = 1;
                }
                else if (UnitReference.GetComponent<ESUnit>().AttackRange > 4)
                {
                    UnitReference.GetComponent<ESUnit>().AttackRange = 4;
                }

                if (UnitReference.GetComponent<ESUnit>().TotalMovementPoints < 1)
                {
                    UnitReference.GetComponent<ESUnit>().TotalMovementPoints = 1;
                }


                var takenCell = cellGrid.Cells.Find(c => (c.transform.localPosition.x.Equals(unitToMerge.transform.localPosition.x) && c.transform.localPosition.y.Equals(unitToMerge.transform.localPosition.y)));
                takenCell.IsTaken = false;

                mergedUnits.Add(unitToMerge);

                unitToMerge.gameObject.SetActive(false);

                SaveAbility.PlayerData toAdd = new SaveAbility.PlayerData();
                toAdd.totalMergeCount = 1;
                
                string mappedUnits = UnitTypes[UnitReference.GetComponent<ESUnit>().UnitName].ToString();
                foreach (var unit in mergedUnits)
                {
                    mappedUnits = mappedUnits + $",{UnitTypes[unit.GetComponent<ESUnit>().UnitName]}";
                }
                for (int i = mergedUnits.Count; i < 4; i++)
                {
                    mappedUnits = mappedUnits + $",{UnitTypes["Empty"]}";
                }

                toAdd.mergeCombinations.Add(mappedUnits, 1);
                cellGrid.CurrentPlayer.GetComponent<SaveAbility>().UpdateValues(toAdd);

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

                        var hoverScript = unitButton.GetComponent<MergeButtonHover>();
                        hoverScript.unitToMerge = unit;

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
            if (MergeButtons.Count > 0)
            {
                var mergeButton = MergeButtons.Find(b => b.GetComponent<MergeButtonHover>().unitToMerge == unitToMerge);
                if (mergeButton != null)
                {
                    mergeButton.GetComponent<MergeButtonHover>().HideMergePreview();
                }
            }

            foreach (var unit in availableMerges)
            {
                unit.UnMark();
            }
            foreach (var button in MergeButtons)
            {
                Destroy(button);
            }
            MergeButtons.Clear();
            MergeUI.SetActive(false);
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
