using UnityEngine;

namespace TbsFramework.Grid.TurnResolvers
{
    public abstract class TurnResolver : MonoBehaviour
    {
        public abstract TransitionResult ResolveStart(CellGrid cellGrid);
        public abstract TransitionResult ResolveTurn(CellGrid cellGrid);
    }
}