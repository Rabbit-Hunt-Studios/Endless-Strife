using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TbsFramework.Cells;
using UnityEngine.Tilemaps;

namespace EndlessStrife.Cells 
{
    public class ESSquare : Square
    {
        public string TileType;
        public int DefenceBoost;

        private Vector3 dimensions = new Vector3(0.16f, 0.16f, 0);

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
