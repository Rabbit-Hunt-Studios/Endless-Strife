using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.HOMMExample
{
    public class SpellCastingAbility : Ability
    {
        public GameObject SpellBookPanel;
        public GameObject SpellPanel;
        public GameObject CancelButton;
        public List<SpellAbility> Spells;
        public List<GameObject> SpellPanels;

        public int MaxMana;
        public int CurrentMana { get; set; }
        public int ManaRecoveryRate;


        public override void Initialize()
        {
            CurrentMana = MaxMana;
            SpellPanels = new List<GameObject>();
            foreach (var spell in Spells)
            {
                UnitReference.RegisterAbility(spell);
            }
        }

        public override void Display(CellGrid cellGrid)
        {
            SpellBookPanel.SetActive(true);
            SpellBookPanel.transform.Find("TotalMana").GetComponent<Text>().text = string.Format("{0} Mana", CurrentMana);

            foreach (var spell in Spells)
            {
                var spellPanelInstance = Instantiate(SpellPanel);
                spellPanelInstance.transform.parent = SpellPanel.transform.parent;
                spellPanelInstance.transform.Find("SpellName").GetComponent<Text>().text = spell.GetComponent<SpellAbility>().SpellName;
                spellPanelInstance.transform.Find("Details").GetComponent<Text>().text = spell.GetComponent<SpellAbility>().GetDetails();
                spellPanelInstance.GetComponent<SpellDetails>().Spell = spell;
                spellPanelInstance.GetComponentInChildren<Image>().sprite = spell.GetComponent<SpellAbility>().Image;
                spellPanelInstance.GetComponentInChildren<Image>().color = spell.GetComponent<SpellAbility>().ManaCost <= CurrentMana ? Color.white : Color.gray;
                spellPanelInstance.SetActive(true);
                spellPanelInstance.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    if (CurrentMana >= spell.ManaCost)
                    {
                        CancelButton.SetActive(true);
                        cellGrid.cellGridState = new CellGridStateAbilitySelected(cellGrid, UnitReference, new List<Ability>() { spell });
                    }
                });

                SpellPanels.Add(spellPanelInstance);
            }
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            foreach (var panel in SpellPanels)
            {
                Destroy(panel);
            }

            SpellBookPanel.SetActive(false);
            SpellPanels = new List<GameObject>();
        }

        public override void OnTurnStart(CellGrid cellGrid)
        {
            CurrentMana = Mathf.Min(CurrentMana + ManaRecoveryRate, MaxMana);
            foreach (var spell in Spells)
            {
                spell.OnTurnStart(cellGrid);
            }
        }

        public override void OnTurnEnd(CellGrid cellGrid)
        {
            foreach (var spell in Spells)
            {
                spell.OnTurnEnd(cellGrid);
            }
        }

        public void CancelCasting()
        {
            var cellGrid = FindObjectOfType<CellGrid>();
            cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid);
            CancelButton.SetActive(false);
        }
    }
}