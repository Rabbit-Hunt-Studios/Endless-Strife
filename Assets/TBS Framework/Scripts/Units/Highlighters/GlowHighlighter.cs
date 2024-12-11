using System.Collections;
using UnityEngine;

namespace TbsFramework.Units.Highlighters
{
    public class GlowHighlighter : UnitHighlighter
    {
        [SerializeField] private Color Color;
        [SerializeField] private float CooloutTime;
        [SerializeField] private Renderer Renderer;
        [SerializeField] private string PropertyName = "_Color";

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        public override void Apply(Unit unit, Unit otherUnit)
        {
            StartCoroutine(Glow());
        }

        private IEnumerator Glow()
        {
            float endTime = Time.time + CooloutTime;
            Renderer.GetPropertyBlock(_mpb);
            Color baseColor = Renderer.sharedMaterial.color;

            while (Time.time < endTime)
            {
                _mpb.SetColor(PropertyName, Color.Lerp(baseColor, Color, (endTime - Time.time) / CooloutTime));
                Renderer.SetPropertyBlock(_mpb);
                yield return null;
            }

            _mpb.SetColor(PropertyName, baseColor);
            Renderer.SetPropertyBlock(_mpb);
        }
    }
}
