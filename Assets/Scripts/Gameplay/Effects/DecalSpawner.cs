// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using UnityEngine;

namespace Game.Gameplay.Effects
{
    public class DecalSpawner : MonoBehaviour
    {
        public GameObject xDecalPrefab;
        public void PlaceX(Vector3 pos)
        {
            if (!xDecalPrefab) return;
            var d = Instantiate(xDecalPrefab, pos, Quaternion.identity);
            Destroy(d, 2f);
        }
    }
}
