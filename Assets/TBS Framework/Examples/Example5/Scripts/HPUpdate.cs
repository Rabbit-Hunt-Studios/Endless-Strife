using TbsFramework.Units;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.HOMMExample
{
    public class HPUpdate : MonoBehaviour
    {
        public Text HPText;

        void Start()
        {
            GetComponent<Unit>().UnitAttacked += OnUnitAttacked;
        }

        private void OnUnitAttacked(object sender, AttackEventArgs e)
        {
            HPText.text = e.Defender.HitPoints.ToString();
        }
    }
}

