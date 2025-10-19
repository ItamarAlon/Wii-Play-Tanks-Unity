
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public class HUDController : MonoBehaviour
    {
        public TMP_Text livesText;
        public TMP_Text stageText;
        public TMP_Text killsText;

        public void Disable()
        {
            if (livesText) livesText.gameObject.SetActive(false);
            if (stageText) stageText.gameObject.SetActive(false);
            if (killsText) killsText.gameObject.SetActive(false);
        }

        public void SetLives(int livesNum)
        { 
            if (livesText) livesText.text = $"Lives {livesNum}"; 
        }
        public void SetStage(int stageNum) 
        { 
            if (stageText) stageText.text = $"Stage {stageNum}"; 
        }
        public void SetKills(int killsNum)
        { 
            if (killsText) killsText.text = killsNum.ToString(); 
        }
    }
}
