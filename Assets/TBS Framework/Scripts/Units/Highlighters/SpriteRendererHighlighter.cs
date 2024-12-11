using UnityEngine;

namespace TbsFramework.Units.Highlighters
{
    public class SpriteRendererHighlighter : UnitHighlighter
    {
        public SpriteRenderer Renderer;
        public Color Color;

        public override void Apply(Unit unit, Unit otherUnit)
        {
            Renderer.color = Color;
        }
    }
}