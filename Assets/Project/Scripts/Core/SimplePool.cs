using System.Collections.Generic;
using UnityEngine;

namespace WhaleShark.Core
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    public class SimplePool : MonoBehaviour
    {
        [SerializeField] GameObject prefab;
        [SerializeField] int warmCount = 8;
        readonly Queue<GameObject> q = new Queue<GameObject>();
        readonly Dictionary<GameObject, IPoolable> poolableCache = new Dictionary<GameObject, IPoolable>();

        void Awake()
        {
            WarmUp(warmCount);
        }

        public void WarmUp(int count)
        {
            for(int i = 0; i < count; i++)
            {
                var go = Instantiate(prefab, transform);
                go.SetActive(false);
                
                // 생성 시점에 IPoolable 캐싱
                var poolable = go.GetComponent<IPoolable>();
                if(poolable != null)
                    poolableCache[go] = poolable;
                
                q.Enqueue(go);
            }
        }

        public GameObject Spawn(Vector3 pos, Quaternion rot)
        {
            GameObject go;
            if(q.Count > 0)
            {
                go = q.Dequeue();
            }
            else
            {
                go = Instantiate(prefab, transform);
                // 새로 생성된 오브젝트도 캐싱
                var poolable = go.GetComponent<IPoolable>();
                if(poolable != null)
                    poolableCache[go] = poolable;
            }
            
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            
            if(poolableCache.TryGetValue(go, out var cached))
                cached.OnSpawned();
            
            return go;
        }

        public void Despawn(GameObject go)
        {
            if(poolableCache.TryGetValue(go, out var cached))
                cached.OnDespawned();
            
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            q.Enqueue(go);
        }
    }
}