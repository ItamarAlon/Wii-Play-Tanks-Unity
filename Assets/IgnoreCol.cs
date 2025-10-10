using Game.Gameplay.Projectiles;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class IgnoreCol : MonoBehaviour
{
    public List<Collider2D> componentsToIgnoreCollisionWith = new List<Collider2D>();
    void Awake()
    {
        Collider2D collider = gameObject.GetComponent<Collider2D>();
        foreach (Collider2D component in componentsToIgnoreCollisionWith)
        {
            //Collider2D otherCollider = component.gameObject.GetComponent<Collider2D>();
            //if (otherCollider != null)
            //    Physics2D.IgnoreCollision(collider, otherCollider);
            Physics2D.IgnoreCollision(collider, component);
        }
    }

    void Update()
    {
        
    }
}
