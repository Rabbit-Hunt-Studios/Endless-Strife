using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TbsFramework.Players
{
    public class SetAILevel : MonoBehaviour
    {
        public GameObject player;
        public int defaultDepth = 3;

        void Start()
        {
            if (PlayerPrefs.HasKey("MinimaxDepth"))
            {
                player.GetComponent<RLMinimaxPlayer>().maxDepth = PlayerPrefs.GetInt("MinimaxDepth");
            }
            else
            {
                player.GetComponent<RLMinimaxPlayer>().maxDepth = defaultDepth;
            }
        }
    }
}
