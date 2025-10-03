// Auto-generated stubs for Wii Play Tanks
// Generated: 2025-10-03T10:21:31.691001
// You can safely replace any class body with the real implementation from the HTML guide.

using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _pool = new Stack<T>();

        public ObjectPool(T prefab, Transform parent = null, int initial = 0)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public T Get()
        {
            if (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                obj.gameObject.SetActive(true);
                return obj;
            }
            return GameObject.Instantiate(_prefab, _parent);
        }

        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }
    }
}
