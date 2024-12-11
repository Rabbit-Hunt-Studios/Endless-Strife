using UnityEngine;

namespace TbsFramework.Cells.Highlighters
{
    public class CellRendererHighlighter : CellHighlighter
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

        public override void Apply(Cell cell)
        {
            Renderer.SetPropertyBlock(_mpb);
        }
    }
}