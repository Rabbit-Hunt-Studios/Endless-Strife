using TbsFramework.Units;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.Example1
{
    public class MyUnit : Unit
    {
        public string UnitName;
        private Transform Highlighter;

        public override void Initialize()
        {
            base.Initialize();
            Highlighter = transform.Find("Highlighter");
        }

        protected override void DefenceActionPerformed()
        {
            UpdateHpBar();
        }

        private void UpdateHpBar()
        {
            var healthBar = transform.Find("HealthCanvas").Find("HealthBar");
            if (healthBar != null)
            {
                healthBar.GetComponent<Image>().transform.localScale = new Vector3((float)(HitPoints / (float)TotalHitPoints), 1, 1);
                healthBar.GetComponent<Image>().color = Color.Lerp(Color.red, Color.green,
                    (float)(HitPoints / (float)TotalHitPoints));
            }
        }
        private void SetHighlighterColor(Color color)
        {
            Highlighter.GetComponent<Renderer>().material.color = color;
        }

        public override void SetColor(Color color)
        {
            SetHighlighterColor(color);
        }
    }
}