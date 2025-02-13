using System.Collections;
using System.Collections.Generic;
using TbsFramework.Units;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MergeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite IncreaseSprite;
    public Sprite DecreaseSprite;
    public Sprite UnchangedSprite;
    public GameObject StatPanel;
    public GameObject StatCard;
    private List<GameObject> StatDisplays = new List<GameObject>();
    public Unit unitToMerge;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowMergePreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideMergePreview();
    }

    public Sprite GetSprite(int value)
    {
        if (value > 0)
        {
            return this.IncreaseSprite;
        }
        else if (value < 0)
        {
            return this.DecreaseSprite;
        }
        return this.UnchangedSprite;
    }

    public void ShowMergePreview()
    {
        var unit = unitToMerge.GetComponent<MergeStats>();

        var healthCard = Instantiate(StatCard, StatCard.transform.parent);
        healthCard.transform.GetChild(0).GetComponent<Image>().sprite = GetSprite(unit.HitPoints);
        healthCard.transform.GetChild(1).GetComponent<Text>().text = unit.HitPoints.ToString();
        healthCard.SetActive(true);
        StatDisplays.Add(healthCard);

        var attackCard = Instantiate(StatCard, StatCard.transform.parent);
        attackCard.transform.GetChild(0).GetComponent<Image>().sprite = GetSprite(unit.Attack);
        attackCard.transform.GetChild(1).GetComponent<Text>().text = unit.Attack.ToString();
        attackCard.SetActive(true);
        StatDisplays.Add(attackCard);

        var defenceCard = Instantiate(StatCard, StatCard.transform.parent);
        defenceCard.transform.GetChild(0).GetComponent<Image>().sprite = GetSprite(unit.Defence);
        defenceCard.transform.GetChild(1).GetComponent<Text>().text = unit.Defence.ToString();
        defenceCard.SetActive(true);
        StatDisplays.Add(defenceCard);

        var rangeCard = Instantiate(StatCard, StatCard.transform.parent);
        rangeCard.transform.GetChild(0).GetComponent<Image>().sprite = GetSprite(unit.AttackRange);
        rangeCard.transform.GetChild(1).GetComponent<Text>().text = unit.AttackRange.ToString();
        rangeCard.SetActive(true);
        StatDisplays.Add(rangeCard);

        var moveCard = Instantiate(StatCard, StatCard.transform.parent);
        moveCard.transform.GetChild(0).GetComponent<Image>().sprite = GetSprite(unit.Movement);
        moveCard.transform.GetChild(1).GetComponent<Text>().text = unit.Movement.ToString();
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
}
