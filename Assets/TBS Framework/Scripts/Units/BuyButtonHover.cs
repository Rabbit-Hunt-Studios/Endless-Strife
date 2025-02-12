using TbsFramework.Units;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject unitPrefab;
    private SpawnAbility spawnAbility;

    public void Initialize(GameObject unitPrefab, SpawnAbility spawnAbility)
    {
        this.unitPrefab = unitPrefab;
        this.spawnAbility = spawnAbility;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        spawnAbility.ShowStats(unitPrefab);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        spawnAbility.HideStats();
    }
}
