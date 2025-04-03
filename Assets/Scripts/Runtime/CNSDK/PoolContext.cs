using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace CreateNeptune
{
    /// <summary>
    /// A utility class to reduce the amount of things you have worry about with pooling
    /// Maybe should be added to the sdk at some point
    /// </summary>
    [Serializable]
    public class PoolContext
    {
        /// <summary>
        /// The list that contains all the objects in this pool
        /// </summary>
        private List<GameObject> pool;
        /// <summary>
        /// The prefab for the pool
        /// </summary>
        [SerializeField] private GameObject prefab;
        /// <summary>
        /// The default parent object for the pool
        /// </summary>
        [SerializeField] private Transform parent;
        [SerializeField] private int initPoolSize;

        public PoolContext(GameObject prefab, Transform parent, int initPoolSize)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.initPoolSize = initPoolSize;

            Initialize();
        }

        public void Initialize()
        {
            if (prefab != null && parent != null)
            {
                pool = new List<GameObject>();
                CNExtensions.CreateObjectPool(pool, prefab, initPoolSize, parent);
            }
        }

        public GameObject GetPooledObject()
        {
            return CNExtensions.GetPooledObject(pool, prefab, parent);
        }

        public T GetPooledObject<T>() where T : Component
        {
            return CNExtensions.GetPooledObject<T>(pool, prefab, parent);
        }

        /// <summary>
        /// Returns all the active objects to the pool
        /// </summary>
        public void ReturnAllToPool()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                pool[i].SetActive(false);
            }
        }
    }
}