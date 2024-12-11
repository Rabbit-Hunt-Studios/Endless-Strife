using UnityEngine;

namespace TbsFramework.Cells.Highlighters
{
    public class CellSpriteRendererHighlighter : CellHighlighter
    {
        public SpriteRenderer Renderer;
        public Color Color;

        public override void Apply(Cell cell)
        {
            Renderer.color = Color;
        }
    }
}