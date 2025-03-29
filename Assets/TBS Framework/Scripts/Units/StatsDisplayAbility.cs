using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using TbsFramework.Grid;

public class StatsDisplayAbility : Ability
{
    public Sprite AttackSprite;
    public Sprite DefenceSprite;
    public Sprite RangeSprite;
    public Sprite MovementSprite;
    public Sprite HitpointsSprite;
    public GameObject StatCard;
    public GameObject StatPanel;
    public GameObject UnitNameCard;
    public GameObject MergedIconList;
    private List<GameObject> StatDisplays = new List<GameObject>();
    private bool destroy = true;

    public override void Display(CellGrid cellGrid)
    {
        // unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = unit.GetComponent<SpriteRenderer>().sprite;
        // unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = unit.GetComponent<ESUnit>().UnitName;

        // unitButton.SetActive(true);
        // MergeButtons.Add(unitButton);
        // UnitPanel.SetActive(true);
        // var StatsText = Instantiate(UnmergeButton, UnmergeButton.transform.parent);
        destroy_cards();
        create_stat_panel();
        destroy = false;
    }

    public void create_stat_panel()
    {
        if (StatDisplays.Count != 0)
        {
            return;
        }
        var unitCard = Instantiate(UnitNameCard, UnitNameCard.transform.parent);
        unitCard.transform.GetChild(0).GetComponent<Image>().sprite = UnitReference.GetComponent<SpriteRenderer>().sprite;
        unitCard.transform.GetChild(1).GetComponent<Text>().text = UnitReference.GetComponent<ESUnit>().UnitName;
        unitCard.SetActive(true);
        StatDisplays.Add(unitCard);

        Dictionary<Sprite, string> stats = new Dictionary<Sprite, string>();

        stats.Add(HitpointsSprite, "Health: " + UnitReference.GetComponent<ESUnit>().HitPoints.ToString());
        stats.Add(AttackSprite, "Attack: " + UnitReference.GetComponent<ESUnit>().AttackFactor.ToString());
        stats.Add(DefenceSprite, "Defence: " + UnitReference.GetComponent<ESUnit>().DefenceFactor.ToString() + " %");
        stats.Add(RangeSprite, "Range: " + UnitReference.GetComponent<ESUnit>().AttackRange.ToString());
        stats.Add(MovementSprite, "Moves: " + UnitReference.GetComponent<ESUnit>().MovementPoints.ToString());

        foreach(KeyValuePair<Sprite, string> ele in stats)
        {
            create_card(ele.Key, ele.Value);
        }

        var unit_list = UnitReference.GetComponent<MergeAbility>().mergedUnits;
        if (unit_list.Count != 0) 
        {
            var iconList = Instantiate(MergedIconList, MergedIconList.transform.parent);
            for(int i = 0; i < unit_list.Count; i++)
            {   
                iconList.transform.GetChild(i).GetComponent<Image>().sprite = unit_list[i].GetComponent<ESUnit>().UnitWeaponIcon;
            }
            StatDisplays.Add(iconList);
            iconList.SetActive(true);
        }

        StatPanel.SetActive(true);
    }

    public void create_card(Sprite sprite, string text)
    {
        var newCard = Instantiate(StatCard, StatCard.transform.parent);
        newCard.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
        newCard.transform.GetChild(1).GetComponent<Text>().text = text;
        newCard.SetActive(true);
        StatDisplays.Add(newCard);
    }

    public override void CleanUp(CellGrid cellGrid)
    {
        destroy = true;
        destroy_cards();
    }

    public void destroy_cards()
    {
        if (!destroy)
        {
            return;
        }
        foreach (var card in StatDisplays)
        {
            Destroy(card);
        }
        StatDisplays.Clear();
        StatPanel.SetActive(false);
        MergedIconList.SetActive(false);
    }
}
