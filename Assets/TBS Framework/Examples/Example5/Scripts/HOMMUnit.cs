using TbsFramework.Cells;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public class HOMMUnit : Unit
    {
        public Vector3 offset;
        public bool IsHero;

        public string UnitName;

        public override void Initialize()
        {
            base.Initialize();
            transform.position += offset;
        }

        protected override void OnMoveFinished()
        {
            GetComponentInChildren<SpriteRenderer>().sortingOrder = (int)(Cell.OffsetCoord.x * Cell.OffsetCoord.y);
        }

        public override bool IsUnitAttackable(Unit other, Cell otherCell, Cell sourceCell)
        {
            return otherCell != null && base.IsUnitAttackable(other, otherCell, sourceCell);
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();
        }
    }
}