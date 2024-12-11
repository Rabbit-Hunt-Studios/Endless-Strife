using UnityEngine;

namespace TbsFramework.Units.Highlighters
{
    public class RendererHighlighter : UnitHighlighter
    {
        [SerializeField] private Renderer Renderer;
        [SerializeField] private Color Color;
        [SerializeField] private string PropertyName = "_Color";

        private MaterialPropertyBlock _mpb;
        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _mpb.SetColor(PropertyName, Color);
        }

        public override void Apply(Unit cell, Unit otherUnit)
        {
            Renderer.SetPropertyBlock(_mpb);
        }
    }
}