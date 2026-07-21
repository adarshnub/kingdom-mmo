using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Core
{
    public sealed class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly Queue<T> available = new();
        private readonly T prefab;
        private readonly Transform parent;

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            this.prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            this.parent = parent;
            if (initialSize < 0) throw new ArgumentOutOfRangeException(nameof(initialSize));
            for (int index = 0; index < initialSize; index++) available.Enqueue(Create());
        }

        public T Get()
        {
            T instance = available.Count > 0 ? available.Dequeue() : Create();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            available.Enqueue(instance);
        }

        private T Create()
        {
            T instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}

