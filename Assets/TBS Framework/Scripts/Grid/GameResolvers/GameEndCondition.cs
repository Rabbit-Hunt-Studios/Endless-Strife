using UnityEngine;

namespace TbsFramework.Grid.GameResolvers
{
    public abstract class GameEndCondition : MonoBehaviour
    {
        public abstract GameResult CheckCondition(CellGrid cellGrid);
    }
}
