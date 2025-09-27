using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "WhaleShark/Game/Reward Database", fileName = "RewardDatabase")]
public class RewardDatabase : ScriptableObject
{
    [Tooltip("게임에서 사용할 RewardData 목록")]
    public List<RewardData> rewards = new();

    private Dictionary<string, RewardData> _cacheById;

    private void OnValidate()
    {
        // Null 정리
        rewards.RemoveAll(r => r == null);
        // ID 중복 경고
        var dupGroups = rewards.Where(r => !string.IsNullOrWhiteSpace(r.id))
            .GroupBy(r => r.id)
            .Where(g => g.Count() > 1);
        foreach (var g in dupGroups)
        {
            Debug.LogWarning($"[RewardDatabase] 중복 ID 발견: {g.Key} (count={g.Count()})", this);
        }
        BuildCache();
    }

    private void BuildCache()
    {
        _cacheById = new Dictionary<string, RewardData>();
        foreach (var r in rewards)
        {
            if (r == null) continue;
            if (string.IsNullOrWhiteSpace(r.id)) continue;
            if (!_cacheById.ContainsKey(r.id))
            {
                _cacheById.Add(r.id, r);
            }
        }
    }

    public RewardData GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (_cacheById == null) BuildCache();
        _cacheById.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// 유효한(Null 아니고 id 존재) RewardData만 필터링
    /// </summary>
    public IEnumerable<RewardData> EnumerateValid()
    {
        foreach (var r in rewards)
        {
            if (r == null) continue;
            yield return r;
        }
    }

    public List<RewardData> GetRandomDistinct(int count, bool weightByRarity = true)
    {
        var pool = new List<RewardData>();
        foreach (var r in rewards)
        {
            if (r != null) pool.Add(r);
        }
        if (pool.Count == 0) return new List<RewardData>();
        if (count >= pool.Count)
        {
            Shuffle(pool);
            return new List<RewardData>(pool);
        }

        var result = new List<RewardData>(count);
        if (!weightByRarity)
        {
            Shuffle(pool);
            for (int i = 0; i < count; i++) result.Add(pool[i]);
            return result;
        }

        // 가중치 기반 (각 선택마다 풀에서 제거)
        for (int pick = 0; pick < count && pool.Count > 0; pick++)
        {
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                int w = Mathf.Max(1, pool[i].rarityWeight);
                totalWeight += w;
            }
            int roll = Random.Range(0, totalWeight);
            int accum = 0;
            int chosenIndex = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                accum += Mathf.Max(1, pool[i].rarityWeight);
                if (roll < accum)
                {
                    chosenIndex = i;
                    break;
                }
            }
            var chosen = pool[chosenIndex];
            result.Add(chosen);
            pool.RemoveAt(chosenIndex);
        }
        return result;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
