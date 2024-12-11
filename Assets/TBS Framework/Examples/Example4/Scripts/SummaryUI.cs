using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.Example4
{
    public class SummaryUI : MonoBehaviour
    {
        public GameObject InfoPanel;
        public Text NeutralBasesText;
        public Text TurnsPassedText;
        private List<GameObject> InfoPanels = new List<GameObject>();
        private Dictionary<int, int> UnitsDestroyed = new Dictionary<int, int>();

        private int turnsPassed;

        private void Awake()
        {
            FindObjectOfType<CellGrid>().GameStarted += OnGameStarted;
            FindObjectOfType<CellGrid>().TurnEnded += OnTurnEnded; ;
        }

        private void OnTurnEnded(object sender, bool isNetworkInvoked)
        {
            turnsPassed++;
        }

        private void OnGameStarted(object sender, System.EventArgs e)
        {
            foreach (var factory in FindObjectsOfType<SpawnAbility>())
            {
                factory.UnitSpawned += OnUnitSpawned;
            }
            foreach (var player in FindObjectOfType<CellGrid>().Players)
            {

                UnitsDestroyed.Add(player.PlayerNumber, 0);
            }
        }

        private void OnUnitSpawned(object sender, System.EventArgs e)
        {
            (sender as GameObject).GetComponent<Unit>().UnitDestroyed += OnUnitDestroyed;
        }

        private void OnUnitDestroyed(object sender, AttackEventArgs e)
        {
            UnitsDestroyed[(sender as Unit).PlayerNumber] += 1;
        }

        public void UpdateUI()
        {
            var cellGrid = FindObjectOfType<CellGrid>();
            var players = cellGrid.Players.OrderBy(p => p.PlayerNumber).ToList();
            for (int i = players.Count - 1; i >= 0; i--)
            {
                Players.Player player = cellGrid.Players[i];
                var newInfoPanel = Instantiate(InfoPanel);
                newInfoPanel.transform.SetParent(InfoPanel.transform.parent, false);
                newInfoPanel.transform.SetSiblingIndex(3);
                newInfoPanel.SetActive(true);

                var playerText = newInfoPanel.transform.Find("PlayerText");
                playerText.GetComponentInChildren<Text>().text = string.Format("Player {0}", player.PlayerNumber);

                var unitsText = newInfoPanel.transform.Find("UnitsText");
                unitsText.GetComponentInChildren<Text>().text = cellGrid.Units.FindAll(u => u.PlayerNumber.Equals(player.PlayerNumber) && !(u as AdvWrsUnit).isStructure).Count.ToString();

                var unitsDestroyedText = newInfoPanel.transform.Find("UnitsDestroyedText");
                unitsDestroyedText.GetComponentInChildren<Text>().text = UnitsDestroyed[player.PlayerNumber].ToString();

                var basesText = newInfoPanel.transform.Find("BasesText");
                basesText.GetComponentInChildren<Text>().text = cellGrid.Units.FindAll(u => u.PlayerNumber.Equals(player.PlayerNumber) && (u as AdvWrsUnit).isStructure).Count.ToString();

                var incomeText = newInfoPanel.transform.Find("IncomeText");
                incomeText.GetComponentInChildren<Text>().text = cellGrid.Units.FindAll(u => u.PlayerNumber.Equals(player.PlayerNumber) && u.GetComponent<IncomeGenerationAbility>() != null).Sum(u => u.GetComponent<IncomeGenerationAbility>().Amount).ToString();

                var fundsText = newInfoPanel.transform.Find("FundsText");
                fundsText.GetComponentInChildren<Text>().text = FindObjectOfType<EconomyController>().GetValue(player.PlayerNumber).ToString();

                newInfoPanel.GetComponent<Image>().color = player.GetComponent<ColorComponent>().Color;

                InfoPanels.Add(newInfoPanel);
            }

            NeutralBasesText.text = cellGrid.Units.FindAll(u => u.PlayerNumber.Equals(-1) && (u as AdvWrsUnit).isStructure).Count.ToString();
            TurnsPassedText.text = string.Format("Day {0}", (turnsPassed / FindObjectOfType<CellGrid>().Players.Count) + 1);
        }

        public void Cleanup()
        {
            foreach (var panel in InfoPanels)
            {
                Destroy(panel);
            }

            InfoPanels = new List<GameObject>();
        }
    }
}

