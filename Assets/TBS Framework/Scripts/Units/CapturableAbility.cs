using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Units
{
    public class CapturableAbility : Ability
    {

        public void Capture(int playerNumber)
        {
            GetComponent<Unit>().PlayerNumber = playerNumber;

            var player = FindObjectOfType<CellGrid>().Players.Find(p => p.PlayerNumber == playerNumber);
            FindObjectOfType<CellGrid>().CheckGameFinished();
        }
    }
}
