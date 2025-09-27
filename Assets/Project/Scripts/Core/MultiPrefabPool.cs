using System.Collections.Generic;
 using JetBrains.Annotations;
 using UnityEngine;
 using System.Linq;

namespace WhaleShark.Core
{
    public class MultiPrefabPool : SimplePool
    {
        [System.Serializable]
        public class PrefabEntry
        {
            public GameObject prefab;
            [Min(0)] public int initialWarm = 0;
            [Min(0f)] public float weight = 1f;
            
            [Tooltip("큐가 비었을 때 새 인스턴스 생성 허용 여부")] 
            public bool expandable = true;
        }

        [Header("Multi Prefab Settings")] 
        [SerializeField] private List<PrefabEntry> prefabEntries = new List<PrefabEntry>();

        [SerializeField] private Transform parent;

        float totalWeight;

        protected override void Awake()
        {
            RecalculateTotalWeight();
            WarmUpAll();

            if (parent == null)
                parent = this.transform;
        }

        void RecalculateTotalWeight()
        {
            totalWeight = 0f;
            foreach (var e in prefabEntries)
            {
                if (e?.prefab == null) continue;
                if (e.weight > 0f) totalWeight += e.weight;
            }
        }

        void WarmUpAll()
        {
            foreach (var e in prefabEntries)
            {
                if (e == null || e.prefab == null) continue;
                if (e.initialWarm <= 0) continue;
                var q = GetOrCreateQueue(e.prefab);
                for (int i = 0; i < e.initialWarm; i++)
                {
                    var go = CreateInstance(e.prefab);
                    go.SetActive(false);
                    q.Enqueue(go);
                }
            }
        }

        PrefabEntry PickEntry()
        {
            if (prefabEntries.Count == 0) return null;

            float r = Random.value * totalWeight;
            float acc = 0f;
            foreach (var e in prefabEntries)
            {
                if (e?.prefab == null || e.weight <= 0f) continue;
                acc += e.weight;
                if (r <= acc)
                    return e;
            }
            // 부동소수 오차 fallback
            for (int i = prefabEntries.Count - 1; i >= 0; i--)
            {
                var e = prefabEntries[i];
                if (e?.prefab != null) return e;
            }
            return null;
        }

        protected override GameObject GetPrefabForSpawn()
        {
            var entry = PickEntry();
            return entry?.prefab;
        }

        public override GameObject Spawn(Vector3 pos, Quaternion rot)
        {
            var p = GetPrefabForSpawn();
            if (p == null)
            {
                Debug.LogWarning($"[MultiPrefabPool] Spawn 실패: 선택된 프리팹이 null (풀 {name})");
                return null;
            }
            return SpawnSpecificInternal(p, pos, rot, allowCreate:true);
        }

        /// <summary>
        /// 특정 프리팹을 명시적으로 스폰. 허용되지 않은 prefab 이면 null.
        /// </summary>
        public GameObject SpawnSpecific(GameObject prefab, Vector3 pos, Quaternion rot, bool allowCreate = true)
        {
            if (prefab == null) return null;
            // 등록된 프리팹인지 확인
            var entry = prefabEntries.Find(e => e != null && e.prefab == prefab);
            if (entry == null)
            {
                Debug.LogWarning($"[MultiPrefabPool] 요청된 프리팹 {prefab.name} 은 풀에 등록되지 않음");
                return null;
            }
            return SpawnSpecificInternal(prefab, pos, rot, allowCreate && entry.expandable);
        }

        GameObject SpawnSpecificInternal(GameObject p, Vector3 pos, Quaternion rot, bool allowCreate)
        {
            var q = GetOrCreateQueue(p);
            GameObject go = null;
            if (q.Count > 0)
            {
                go = q.Dequeue();
            }
            else if (allowCreate)
            {
                go = CreateInstance(p);
            }
            else
            {
                // 재사용 가능한 인스턴스 없음
                return null;
            }

            go.transform.SetPositionAndRotation(pos, rot);
            if (parent != null)
                go.transform.SetParent(parent, true);
            else
                go.transform.SetParent(transform, true);

            go.SetActive(true);
            if (poolableCache.TryGetValue(go, out var cached))
                cached.OnSpawned();
            return go;
        }

        /// <summary>
        /// 런타임에 가중치 변경 후 다시 합산할 때 호출.
        /// </summary>
        public void RebuildWeights()
        {
            RecalculateTotalWeight();
        }

        /// <summary>
        /// 현재 활성 상태(씬에 사용 중)인 객체 목록 반환.
        /// 큐(비활성 풀)에 들어있는 객체는 제외.
        /// parent 트랜스폼 자식 + activeSelf 로 판별. (Despawn 시 SimplePool.Despawn 이 SetParent(transform) + SetActive(false) 처리)
        /// </summary>
        public List<GameObject> GetActiveObjects(bool includeInactive = false)
        {
            var list = new List<GameObject>();
            if (parent == null) parent = transform;
            foreach (var kv in instanceToPrefab)
            {
                var go = kv.Key;
                if (go == null) continue;
                // 활성 판정: 부모가 parent 이고 activeSelf (또는 includeInactive 허용)
                if (go.transform.parent == parent && (includeInactive || go.activeSelf))
                {
                    // 큐에 들어가 있지 않은지 한 번 더 방어적 체크 (비활성인데 includeInactive=true 인 경우 걸러질 수 있음)
                    if (!go.activeSelf && !includeInactive) continue;
                    list.Add(go);
                }
            }
            return list;
        }

#if UNITY_EDITOR
        // 에디터에서 배열 정합성 간단 검증 (중복 프리팹 경고 등)
        void OnValidate()
        {
            // total weight 갱신
            RecalculateTotalWeight();
            // 중복 체크 (선택)
            var set = new HashSet<GameObject>();
            foreach (var e in prefabEntries)
            {
                if (e == null || e.prefab == null) continue;
                if (!set.Add(e.prefab))
                {
                    // 단순 경고
                    Debug.LogWarning($"[MultiPrefabPool] 중복 프리팹: {e.prefab.name}");
                }
            }
        }
#endif
    }
}
