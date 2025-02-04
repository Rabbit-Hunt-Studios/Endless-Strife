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
            Debug.Log("Captured by player " + playerNumber);
            GetComponent<Unit>().PlayerNumber = playerNumber;

            var player = FindObjectOfType<CellGrid>().Players.Find(p => p.PlayerNumber == playerNumber);
            var spriteRenderer = GetComponent<ESUnit>().GetComponent<SpriteRenderer>();
            switch (GetComponent<Unit>().PlayerNumber)
            {
                case 0:
                    spriteRenderer.sprite = GetComponent<ESUnit>().Player1Sprite;
                    break;
                case 1:
                    spriteRenderer.sprite = GetComponent<ESUnit>().Player2Sprite;
                    break;
                // Add more cases for additional players
                default:
                    Debug.Log("Default");
                    spriteRenderer.sprite = GetComponent<ESUnit>().DefaultSprite;
                    break;
            }
            FindObjectOfType<CellGrid>().CheckGameFinished();
        }
    }
}
