using TbsFramework.Cells;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TbsFramework.Cells
{
    public class EndlessSquare : Square
     {
        public string TileType;

        Vector3 dimensions = new Vector3(1.6f, 1.6f, 0f);

        public override Vector3 GetCellDimensions()
        {
            return dimensions;
        }

        public override void OnMouseDown()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                base.OnMouseDown();
            }
        }

        public override void OnMouseEnter()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                base.OnMouseEnter();
            }
        }

        public override void OnMouseExit()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                base.OnMouseExit();
            }
        }


        public override void SetColor(Color color)
        {
            var highlighter = transform.Find("marker");
            var spriteRenderer = highlighter.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }
    }
}