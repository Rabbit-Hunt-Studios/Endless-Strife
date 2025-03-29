using TbsFramework.Units;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    
    private UpgradeAbility upgradeAbility;
    private AudioController audioController;

    public void Initialize(UpgradeAbility upgradeAbility)
    {
        this.upgradeAbility = upgradeAbility;
        this.audioController = GameObject.Find("AudioController").GetComponent<AudioController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        audioController.PlaySFX(audioController.ButtonHover);
        upgradeAbility.ShowUpgradeStats();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        upgradeAbility.HideStats();
    }
}
