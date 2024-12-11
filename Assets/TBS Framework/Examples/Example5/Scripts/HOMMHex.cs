using TbsFramework.Cells;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public class HOMMHex : Hexagon
    {
        public override Vector3 GetCellDimensions()
        {
            return new Vector3(5.34f, 4.6f, 0f);
        }

        public override int GetDistance(Cell other)
        {
            return other == null ? int.MaxValue : base.GetDistance(other);
        }

        public override bool Equals(Cell other)
        {
            return other != null && base.Equals(other);
        }

        public override void SetColor(Color color)
        {
            GetComponent<SpriteRenderer>().color = color;
        }
    }
}
