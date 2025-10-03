// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class StageBanner : MonoBehaviour
    {
        public GameObject root;
        public Text line1;
        public Text line2;

        public void Show(int stage, float countdown)
        {
            if (root) root.SetActive(true);
            if (line1) line1.text = $"Stage {stage}";
            if (line2) line2.text = $"Preview: {countdown:0.0}s";
        }

        public void Hide() { if (root) root.SetActive(false); }
    }
}
