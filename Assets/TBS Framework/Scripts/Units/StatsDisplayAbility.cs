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

    public override void Display(CellGrid cellGrid)
    {
        // unitButton.GetComponent<Button>().transform.Find("UnitImage").GetComponent<Image>().sprite = unit.GetComponent<SpriteRenderer>().sprite;
        // unitButton.GetComponent<Button>().transform.Find("NameText").GetComponent<Text>().text = unit.GetComponent<ESUnit>().UnitName;

        // unitButton.SetActive(true);
        // MergeButtons.Add(unitButton);
        // UnitPanel.SetActive(true);
        // var StatsText = Instantiate(UnmergeButton, UnmergeButton.transform.parent);

        var unitCard = Instantiate(UnitNameCard, UnitNameCard.transform.parent);
        unitCard.transform.GetChild(0).GetComponent<Image>().sprite = UnitReference.GetComponent<SpriteRenderer>().sprite;
        unitCard.transform.GetChild(1).GetComponent<Text>().text = UnitReference.GetComponent<ESUnit>().UnitName;
        unitCard.SetActive(true);
        StatDisplays.Add(unitCard);

        var healthCard = Instantiate(StatCard, StatCard.transform.parent);
        healthCard.transform.GetChild(0).GetComponent<Image>().sprite = HitpointsSprite;
        healthCard.transform.GetChild(1).GetComponent<Text>().text = "Health: " + UnitReference.GetComponent<ESUnit>().HitPoints.ToString();
        healthCard.SetActive(true);
        StatDisplays.Add(healthCard);

        var attackCard = Instantiate(StatCard, StatCard.transform.parent);
        attackCard.transform.GetChild(0).GetComponent<Image>().sprite = AttackSprite;
        attackCard.transform.GetChild(1).GetComponent<Text>().text = "Attack: " + UnitReference.GetComponent<ESUnit>().AttackFactor.ToString();
        attackCard.SetActive(true);
        StatDisplays.Add(attackCard);

        var defenceCard = Instantiate(StatCard, StatCard.transform.parent);
        defenceCard.transform.GetChild(0).GetComponent<Image>().sprite = DefenceSprite;
        defenceCard.transform.GetChild(1).GetComponent<Text>().text = "Defence: " + UnitReference.GetComponent<ESUnit>().DefenceFactor.ToString();
        defenceCard.SetActive(true);
        StatDisplays.Add(defenceCard);

        var rangeCard = Instantiate(StatCard, StatCard.transform.parent);
        rangeCard.transform.GetChild(0).GetComponent<Image>().sprite = RangeSprite;
        rangeCard.transform.GetChild(1).GetComponent<Text>().text = "Range: " + UnitReference.GetComponent<ESUnit>().AttackRange.ToString();
        rangeCard.SetActive(true);
        StatDisplays.Add(rangeCard);

        var moveCard = Instantiate(StatCard, StatCard.transform.parent);
        moveCard.transform.GetChild(0).GetComponent<Image>().sprite = MovementSprite;
        moveCard.transform.GetChild(1).GetComponent<Text>().text = "Moves: " + UnitReference.GetComponent<ESUnit>().MovementPoints.ToString();
        moveCard.SetActive(true);
        StatDisplays.Add(moveCard);

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

    public override void CleanUp(CellGrid cellGrid)
    {
        foreach (var card in StatDisplays)
        {
            Destroy(card);
        }
        StatPanel.SetActive(false);
        MergedIconList.SetActive(false);
    }
}
