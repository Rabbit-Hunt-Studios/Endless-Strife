using TbsFramework.Cells;
using UnityEngine;

namespace TbsFramework.Example1
{
    public class MyHexagon : Hexagon
    {
        public Vector3 dimensions = new Vector3(2.2f, 1.9f, 1.1f);

        public override Vector3 GetCellDimensions()
        {
            return dimensions;
        }

        public override void SetColor(Color color)
        {
            GetComponent<Renderer>().material.color = color;
        }
    }
}
