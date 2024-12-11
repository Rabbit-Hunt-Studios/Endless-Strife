using System.Collections.Generic;
using TbsFramework.Grid;
using UnityEngine;
using UnityEngine.Assertions;

namespace TbsFramework.Example4
{
    public class EconomyController : MonoBehaviour
    {
        private Dictionary<int, int> Account = new Dictionary<int, int>();
        public int StartingAmount = 0;

        public void Awake()
        {
            FindObjectOfType<CellGrid>().GameStarted += OnGameStarted;
        }

        private void OnGameStarted(object sender, System.EventArgs e)
        {
            foreach (var player in (sender as CellGrid).Players)
            {
                Account.Add(player.PlayerNumber, StartingAmount);
            }
        }

        public int GetValue(int playerNumber)
        {
            if (Account.ContainsKey(playerNumber))
            {
                return Account[playerNumber];
            }
            return 0;
        }
        public void UpdateValue(int playerNumber, int delta)
        {
            Assert.IsTrue(Account.ContainsKey(playerNumber), string.Format("The Account of player number {0} was not initialized", playerNumber));
            Account[playerNumber] += delta;
        }
    }
}
