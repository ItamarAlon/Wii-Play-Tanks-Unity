
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class StageBanner : MonoBehaviour
    {
        //[SerializeField] private GameObject root;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text countdownText;

        public void Show(int stage, float countdown)
        {
            //if (root) root.SetActive(true);
            if (stageText)
            {
                stageText.gameObject.SetActive(true);
                stageText.text = $"Stage {stage}";
            }
            if (countdownText)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = $"{countdown:0.0}s";
            }
        }

        public void Hide() 
        { 
            //if (root) 
            //    root.SetActive(false); 
            if (stageText) stageText.gameObject.SetActive(false);
            if (countdownText) countdownText.gameObject.SetActive(false);
        }
    }
}
