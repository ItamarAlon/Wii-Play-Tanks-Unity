// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class HUDController : MonoBehaviour
    {
        public Text livesText;
        public Text stageText;
        public Text killsText;

        public void SetLives(int v) { if (livesText) livesText.text = $"Lives: {v}"; }
        public void SetStage(int v) { if (stageText) stageText.text = $"Stage: {v}"; }
        public void SetKills(int v) { if (killsText) killsText.text = $"Kills: {v}"; }
    }
}
