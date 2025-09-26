using UnityEngine;

namespace WhaleShark.Core
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObject = new object();
        private static bool isDestroyed = false;

        public static T Instance
        {
            get
            {
                if (isDestroyed) return null;
                
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = FindFirstObjectByType<T>();
                            if (instance == null)
                            {
                                var go = new GameObject($"[Singleton] {typeof(T).Name}");
                                instance = go.AddComponent<T>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
                isDestroyed = false;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                isDestroyed = true;
            }
        }
    }
}