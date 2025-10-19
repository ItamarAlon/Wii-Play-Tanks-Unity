// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Core
{
    public sealed class ObjectPoolWrapper<T> where T : class
    {
        private readonly ObjectPool<T> pool;
        private readonly List<T> active = new List<T>();
        private readonly Action<T> actionOnDestroy;

        public ObjectPoolWrapper(
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 10000)
        {
            this.actionOnDestroy = actionOnDestroy;
            pool = new ObjectPool<T>(
                createFunc,
                actionOnGet,
                actionOnRelease,
                actionOnDestroy,
                collectionCheck,
                defaultCapacity,
                maxSize
            );
        }

        public IReadOnlyList<T> Active => active;
        public int CountActive => active.Count;
        public int CountInactive => pool.CountInactive;

        public T Get()
        {
            var obj = pool.Get();
            active.Add(obj);
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;
            if (active.Remove(obj))
                pool.Release(obj);
        }

        /// <summary>Releases all active objects back into the pool.</summary>
        public void ReleaseAllActive()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                pool.Release(active[i]);
            active.Clear();
        }

        /// <summary>Destroys ALL objects (both active and inactive) and empties the pool.</summary>
        public void DestroyAll()
        {
            // Destroy actives first (they're not owned by pool until released).
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var o = active[i];
                actionOnDestroy?.Invoke(o);
                if (o is Component c) 
                    UnityEngine.Object.Destroy(c.gameObject);
                else if (o is UnityEngine.Object uo) 
                    UnityEngine.Object.Destroy(uo);
            }
            active.Clear();
        }

        /// <summary>Clears the pool of inactive objects (leaves active ones alone).</summary>
        public void ClearInactive()
        {
            pool.Clear();
        }
    }
}
