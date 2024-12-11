using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Grid.TurnResolvers;
using TbsFramework.Units;
using UnityEngine;
using UnityEngine.UI;

namespace TbsFramework.HOMMExample
{
    public class HOMMGUIController : MonoBehaviour
    {
        public GameObject UnitPanel;
        public Image UnitImage;

        public Text UnitNameText;
        public Text HPText;
        public Text RangeText;
        public Text AttackText;
        public Text DefenceText;
        public Text SpeedText;

        public Text GameOverText;

        private List<string> Logs;
        public List<GameObject> LogsText;

        private void Awake()
        {
            FindObjectOfType<CellGrid>().UnitAdded += OnUnitAdded;
            FindObjectOfType<CellGrid>().GameEnded += OnGameOver;

            Logs = new List<string>();
        }

        private void OnGameOver(object sender, GameEndedArgs e)
        {
            GameOverText.text = string.Format("Player {0} wins!", e.gameResult.WinningPlayers[0]);
            UnitPanel.SetActive(false);

            FindObjectOfType<CellGrid>().UnitAdded -= OnUnitAdded;
            FindObjectOfType<CellGrid>().GameEnded -= OnGameOver;
        }

        private void OnUnitAdded(object sender, TbsFramework.Units.UnitCreatedEventArgs e)
        {
            if (e.unit.GetComponent<HOMMUnit>().IsHero)
            {
                return;
            }

            e.unit.GetComponent<Unit>().UnitHighlighted += OnUnitHighlighted;
            e.unit.GetComponent<Unit>().UnitDehighlighted += OnUnitDehighlighted;
            e.unit.GetComponent<Unit>().UnitAttacked += OnUnitAttacked;
            e.unit.GetComponent<Unit>().UnitMoved += OnUnitMoved;
            e.unit.GetComponent<Unit>().UnitDestroyed += OnUnitDestroyed;
        }

        private void OnUnitDestroyed(object sender, AttackEventArgs e)
        {
            Logs.Add(string.Format("{0}. {1} dies", Logs.Count + 1, (e.Defender as HOMMUnit).UnitName));
            for (int i = Mathf.Max(0, Logs.Count - LogsText.Count), j = 0; i < Logs.Count; i++, j++)
            {
                LogsText[j].GetComponent<Text>().text = Logs[i];
            }

            e.Defender.UnitHighlighted -= OnUnitHighlighted;
            e.Defender.UnitDehighlighted -= OnUnitDehighlighted;
            e.Defender.UnitAttacked -= OnUnitAttacked;
            e.Defender.UnitMoved -= OnUnitMoved;
            e.Defender.UnitDestroyed -= OnUnitDestroyed;
        }

        private void OnUnitMoved(object sender, MovementEventArgs e)
        {
            Logs.Add(string.Format("{0}. {1} moved to {2}", Logs.Count + 1, (e.Unit as HOMMUnit).UnitName, e.DestinationCell.OffsetCoord));
            for (int i = Mathf.Max(0, Logs.Count - LogsText.Count), j = 0; i < Logs.Count; i++, j++)
            {
                LogsText[j].GetComponent<Text>().text = Logs[i];
            }
        }

        private void OnUnitAttacked(object sender, AttackEventArgs e)
        {
            Logs.Add(string.Format("{0}. {1} dealt {2} damage to {3}", Logs.Count + 1, (e.Attacker as HOMMUnit).UnitName, e.Damage, (e.Defender as HOMMUnit).UnitName));
            for (int i = Mathf.Max(0, Logs.Count - LogsText.Count), j = 0; i < Logs.Count; i++, j++)
            {
                LogsText[j].GetComponent<Text>().text = Logs[i];
            }
        }

        private void OnUnitDehighlighted(object sender, System.EventArgs e)
        {
            UnitPanel.SetActive(false);
        }

        private void OnUnitHighlighted(object sender, System.EventArgs e)
        {
            UnitNameText.text = (sender as HOMMUnit).UnitName;
            HPText.text = string.Format("Hit Points: {0}/{1}", (sender as Unit).HitPoints, (sender as Unit).TotalHitPoints);
            AttackText.text = string.Format("Attack: {0}", (sender as Unit).AttackFactor.ToString());
            DefenceText.text = string.Format("Defence: {0}", (sender as Unit).DefenceFactor.ToString());
            RangeText.text = string.Format("Attack Range: {0}", (sender as Unit).AttackRange.ToString());
            SpeedText.text = string.Format("Speed: {0}", (sender as Unit).GetComponent<Speed>().Value.ToString());

            UnitImage.sprite = (sender as Unit).GetComponentInChildren<SpriteRenderer>().sprite;

            UnitPanel.SetActive(true);
        }
    }
}