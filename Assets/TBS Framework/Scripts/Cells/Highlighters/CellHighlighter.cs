using UnityEngine;

namespace TbsFramework.Cells.Highlighters
{
    public abstract class CellHighlighter : MonoBehaviour
    {
        public abstract void Apply(Cell cell);
    }
}