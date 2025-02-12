using TbsFramework.Units;
using UnityEngine;
using UnityEngine.EventSystems;

public class MergeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Unit unitToMerge;
    private MergeAbility mergeAbility;

    public void Initialize(Unit unitToMerge, MergeAbility mergeAbility)
    {
        this.unitToMerge = unitToMerge;
        this.mergeAbility = mergeAbility;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mergeAbility.ShowMergePreview(unitToMerge);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mergeAbility.HideMergePreview();
    }
}
