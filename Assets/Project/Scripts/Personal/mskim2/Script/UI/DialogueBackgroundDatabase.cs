using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Background Database", fileName = "DialogueBackgroundDatabase")]
public class DialogueBackgroundDatabase : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string key;
        public Sprite sprite;
    }

    [Tooltip("Key → Sprite 매핑 목록")]
    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, Sprite> _cache;

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _cache = new Dictionary<string, Sprite>();
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            if (!_cache.ContainsKey(e.key)) _cache.Add(e.key, e.sprite);
        }
    }

    public Sprite Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_cache == null) BuildCache();
        _cache.TryGetValue(key, out var s);
        return s;
    }
}

