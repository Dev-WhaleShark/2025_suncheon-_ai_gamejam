using System.Collections.Generic;
using UnityEngine;

namespace WhaleShark.Core
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// 단일 프리팹 기본 풀. 다중 프리팹 풀 구현을 위해 상속/오버라이드 구조 제공.
    /// 파생 클래스(MultiPrefabPool)는 GetPrefabForSpawn, 커스텀 WarmUp 로직을 재정의.
    /// </summary>
    public class SimplePool : MonoBehaviour
    {
        [Header("Single Prefab Mode")] [SerializeField] protected GameObject prefab; // 기본 단일 프리팹
        [SerializeField] protected int warmCount = 8;

        // prefab -> 사용 가능한 인스턴스 큐
        protected readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        // 인스턴스 -> IPoolable 캐시
        protected readonly Dictionary<GameObject, IPoolable> poolableCache = new Dictionary<GameObject, IPoolable>();
        // 인스턴스 -> 원본 prefab 매핑 (다중 프리팹 지원)
        protected readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

        protected virtual void Awake()
        {
            // 단일 프리팹 지정된 경우만 기본 워밍업
            if (prefab != null && warmCount > 0)
                WarmUp(warmCount);
        }

        /// <summary>
        /// 단일 프리팹 용 워밍업. MultiPrefabPool 에서는 별도 구현 사용.
        /// </summary>
        public virtual void WarmUp(int count)
        {
            var p = GetPrefabForSpawn();
            if (p == null) return;
            var q = GetOrCreateQueue(p);
            for (int i = 0; i < count; i++)
            {
                var go = CreateInstance(p);
                go.SetActive(false);
                q.Enqueue(go);
            }
        }

        /// <summary>
        /// 스폰 시 사용할 프리팹 반환 (파생 클래스에서 오버라이드하여 가중치 선택 등 구현)
        /// </summary>
        protected virtual GameObject GetPrefabForSpawn() => prefab;

        /// <summary>
        /// 프리팹별 큐 얻기 (없으면 생성)
        /// </summary>
        protected Queue<GameObject> GetOrCreateQueue(GameObject p)
        {
            if (!pools.TryGetValue(p, out var q))
            {
                q = new Queue<GameObject>();
                pools[p] = q;
            }
            return q;
        }

        /// <summary>
        /// 인스턴스 생성 + 캐싱
        /// </summary>
        protected virtual GameObject CreateInstance(GameObject sourcePrefab)
        {
            var go = Instantiate(sourcePrefab, transform);
            var poolable = go.GetComponent<IPoolable>();
            if (poolable != null)
                poolableCache[go] = poolable;
            instanceToPrefab[go] = sourcePrefab;
            return go;
        }

        /// <summary>
        /// 위치/회전 지정 스폰
        /// </summary>
        public virtual GameObject Spawn(Vector3 pos, Quaternion rot)
        {
            var p = GetPrefabForSpawn();
            if (p == null)
            {
                Debug.LogWarning($"[SimplePool] Spawn 실패: prefab 이 null (풀 {name})");
                return null;
            }
            var q = GetOrCreateQueue(p);
            GameObject go;
            if (q.Count > 0)
            {
                go = q.Dequeue();
            }
            else
            {
                go = CreateInstance(p);
            }

            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.SetParent(null, true); // 풀 바깥으로 잠시 분리(선택)
            go.SetActive(true);

            if (poolableCache.TryGetValue(go, out var cached))
                cached.OnSpawned();

            return go;
        }

        /// <summary>
        /// 특정 인스턴스 반환
        /// </summary>
        public virtual void Despawn(GameObject go)
        {
            if (go == null) return;
            if (poolableCache.TryGetValue(go, out var cached))
                cached.OnDespawned();

            if (!instanceToPrefab.TryGetValue(go, out var p) || p == null)
            {
                // 알 수 없는 프리팹: 안전하게 풀 기본 부모로만 이동 후 비활성
                go.SetActive(false);
                go.transform.SetParent(transform, false);
                return;
            }
            var q = GetOrCreateQueue(p);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            q.Enqueue(go);
        }
    }
}