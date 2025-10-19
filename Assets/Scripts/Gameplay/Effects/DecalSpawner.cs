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
            //Destroy(d, 2f);
        }
    }
}
