using Game.Gameplay.Projectiles;
using Game.Gameplay.Tanks.Shared;
using UnityEngine;

[ExecuteAlways]
public class TankColor : MonoBehaviour
{
    [SerializeField] Color color = Color.white;
    [SerializeField] bool affectBullet = true;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        if (!this) return;

        var tankParts = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in tankParts)
        {
            if (!s) continue;
            float a = s.color.a;
            s.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(a > 0f ? a : 1f));
        }

        if (affectBullet)
        {
            var shooter = GetComponent<Shooter>();
            if (shooter != null)
                shooter.SetBulletTint(new Color(color.r, color.g, color.b, 1f));
        }
    }
}
