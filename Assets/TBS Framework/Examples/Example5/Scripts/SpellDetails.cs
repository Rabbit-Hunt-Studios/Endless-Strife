using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TbsFramework.HOMMExample
{
    public class SpellDetails : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Text DescriptionText;
        public SpellAbility Spell;

        public void OnPointerEnter(PointerEventData eventData)
        {
            DescriptionText.text = Spell.Description;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DescriptionText.text = "";
        }
    }
}
