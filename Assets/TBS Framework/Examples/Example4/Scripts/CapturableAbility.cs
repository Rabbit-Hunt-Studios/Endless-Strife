using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.Example4
{
    public class CapturableAbility : Ability
    {
        public int MaxLoyality;
        public int Loyality { get; set; }

        public Text LoyalityText;

        private void Start()
        {
            Loyality = MaxLoyality;
            LoyalityText.text = "";
        }

        public override void OnTurnStart(CellGrid cellGrid)
        {
            if (!UnitReference.Cell.CurrentUnits.Exists(u => u.PlayerNumber != UnitReference.PlayerNumber))
            {
                Loyality = MaxLoyality;
                UpdateLoyalityUI();
            }
        }

        public void Capture(int amount, int playerNumber)
        {
            Loyality -= amount;
            if (Loyality <= 0)
            {
                GetComponent<Unit>().PlayerNumber = playerNumber;
                Loyality = MaxLoyality;

                var player = FindObjectOfType<CellGrid>().Players.Find(p => p.PlayerNumber == playerNumber);
                transform.Find("Mask").GetComponent<SpriteRenderer>().color = player.GetComponent<ColorComponent>().Color;
                FindObjectOfType<CellGrid>().CheckGameFinished();
            }

            UpdateLoyalityUI();
        }

        public void UpdateLoyalityUI()
        {
            LoyalityText.text = Loyality == MaxLoyality ? "" : Loyality.ToString();
        }
    }
}
