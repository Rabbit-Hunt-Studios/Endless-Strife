using TbsFramework.Units;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    
    private UpgradeAbility upgradeAbility;

    public void Initialize(UpgradeAbility upgradeAbility)
    {
        this.upgradeAbility = upgradeAbility;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        upgradeAbility.ShowUpgradeStats();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        upgradeAbility.HideStats();
    }
}
